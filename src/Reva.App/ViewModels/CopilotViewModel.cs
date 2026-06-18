using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Reva.Ai;
using Reva.Infrastructure;
using Reva.Infrastructure.Agent;
using Reva.Infrastructure.Persistence;
using Reva.Infrastructure.Review;
using Reva.Infrastructure.Settings;

namespace Reva.App.ViewModels;

public partial class CopilotViewModel : ViewModelBase, IDisposable
{
    private const string AssistantRole = "assistant";
    private const string UserRole = "user";
    private const string TextPartType = "text";
    private const string RoleProperty = "role";
    private const string PartsProperty = "parts";
    private const string TextProperty = "text";
    private const string TypeProperty = "type";
    private const string MessagesProperty = "messages";
    private const string OfflineModel = "Model offline";
    private const string ThinkingText = "Working…";
    private static readonly CompositeFormat RunningFormat = CompositeFormat.Parse("Running {0}…");
    private static readonly CompositeFormat ArgumentFormat = CompositeFormat.Parse("{0}={1}");
    private const string ArgumentSeparator = ", ";
    private const int ArgumentValueMaxLength = 48;
    private const int ResultMaxLength = 160;
    private const string SendErrorText = "The copilot could not complete that request. Deterministic processing still works.";
    private const string EmptyHistoryHint =
        "Ask about ingested bordereaux, reconciliation exceptions, or field citations. The copilot can also navigate the app and act on documents.";
    private const string OfflineHint =
        "Start Ollama and pull a model to enable the copilot; document processing still works without it.";

    private static readonly JsonSerializerOptions HistoryJsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IModelRegistry _modelRegistry;
    private readonly IAgentChatService _agentChat;

    private CancellationTokenSource? _streamCts;

    [ObservableProperty]
    private string _title = "Copilot";

    [ObservableProperty]
    private string _description = EmptyHistoryHint;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCommand))]
    private string _input = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isOnline;

    [ObservableProperty]
    private string _modelStatus = OfflineModel;

    [ObservableProperty]
    private string _activityText = string.Empty;

    [ObservableProperty]
    private bool _hasMessages;

    public CopilotViewModel(IServiceScopeFactory scopeFactory, IModelRegistry modelRegistry, IAgentChatService agentChat)
    {
        _scopeFactory = scopeFactory;
        _modelRegistry = modelRegistry;
        _agentChat = agentChat;
    }

    public ObservableCollection<CopilotMessageViewModel> Messages { get; } = new();

    public ObservableCollection<string> Suggestions { get; } = new()
    {
        "Process the demo documents and open the first with exceptions",
        "Which documents have reconciliation exceptions?",
        "Export the reviewed bordereau as CSV"
    };

    public bool HasActivity => !string.IsNullOrEmpty(ActivityText);

    partial void OnActivityTextChanged(string value) => OnPropertyChanged(nameof(HasActivity));

    public async Task RefreshStatusAsync(CancellationToken cancellationToken = default)
    {
        bool online;
        string? model;
        try
        {
            online = await _modelRegistry.IsOllamaAvailableAsync(cancellationToken);
            model = await _modelRegistry.GetActiveModelAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            online = false;
            model = null;
        }

        if (Dispatcher.UIThread.CheckAccess())
        {
            ApplyStatus(online, model);
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() => ApplyStatus(online, model));
    }

    [RelayCommand(CanExecute = nameof(CanSuggest))]
    private async Task UseSuggestionAsync(string? suggestion)
    {
        if (IsBusy || string.IsNullOrWhiteSpace(suggestion))
        {
            return;
        }

        Input = suggestion;
        await SendAsync();
    }

    private bool CanSuggest() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task SendAsync()
    {
        var prompt = Input.Trim();
        if (prompt.Length == 0 || IsBusy)
        {
            return;
        }

        Input = string.Empty;
        AppendMessage(new CopilotMessageViewModel(CopilotRole.User, prompt));

        var assistant = new CopilotMessageViewModel(CopilotRole.Assistant, string.Empty);
        AppendMessage(assistant);

        IsBusy = true;
        ActivityText = ThinkingText;

        _streamCts?.Dispose();
        _streamCts = new CancellationTokenSource();
        var cancellationToken = _streamCts.Token;

        try
        {
            await RunTurnAsync(assistant, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            assistant.MarkCancelled();
        }
        catch (Exception)
        {
            assistant.AppendText(assistant.Text.Length == 0 ? SendErrorText : Environment.NewLine + SendErrorText);
        }
        finally
        {
            assistant.Complete();
            IsBusy = false;
            ActivityText = string.Empty;
        }
    }

    private bool CanSend() => !IsBusy && !string.IsNullOrWhiteSpace(Input);

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel()
    {
        if (_streamCts is { } cts && !cts.IsCancellationRequested)
        {
            cts.Cancel();
        }
    }

    private bool CanCancel() => IsBusy;

    [RelayCommand]
    private void Clear()
    {
        if (IsBusy)
        {
            return;
        }

        Messages.Clear();
        HasMessages = false;
        Description = IsOnline ? EmptyHistoryHint : OfflineHint;
    }

    public void Dispose()
    {
        _streamCts?.Dispose();
        _streamCts = null;
        GC.SuppressFinalize(this);
    }

    private async Task RunTurnAsync(CopilotMessageViewModel assistant, CancellationToken cancellationToken)
    {
        var parseResult = AgentChatRequestParser.ParseJson(BuildRequestJson());
        if (!parseResult.IsSuccess)
        {
            assistant.AppendText(parseResult.ErrorMessage ?? SendErrorText);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var provider = scope.ServiceProvider;
        var workflow = provider.GetRequiredService<IDocumentWorkflow>();
        var dbContext = provider.GetRequiredService<RevaDbContext>();
        var assembler = provider.GetRequiredService<IBdxReviewPayloadAssembler>();
        var maintenance = provider.GetService<IDataMaintenance>();

        var tools = _agentChat.BuildTools(workflow, dbContext, assembler, cancellationToken, maintenance);
        var produced = false;

        await foreach (var update in _agentChat.StreamAsync(parseResult.Messages, tools, cancellationToken)
            .WithCancellation(cancellationToken))
        {
            foreach (var content in update.Contents)
            {
                switch (content)
                {
                    case TextContent text when text.Text.Length > 0:
                        produced = true;
                        var delta = text.Text;
                        await Dispatcher.UIThread.InvokeAsync(() => assistant.AppendText(delta));
                        break;
                    case FunctionCallContent call:
                        produced = true;
                        var summary = DescribeArguments(call.Arguments);
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            var step = assistant.AddStep(call.CallId, call.Name, summary);
                            ActivityText = string.Format(CultureInfo.InvariantCulture, RunningFormat, step.DisplayName);
                        });
                        break;
                    case FunctionResultContent result:
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            assistant.CompleteStep(result.CallId, DescribeResult(result.Result));
                            ActivityText = ThinkingText;
                        });
                        break;
                }
            }
        }

        if (!produced && assistant.Text.Length == 0)
        {
            assistant.AppendText(IsOnline ? SendErrorText : AgentStreamConstants.UnavailableMessage);
        }
    }

    private string BuildRequestJson()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
        {
            writer.WriteStartObject();
            writer.WriteStartArray(MessagesProperty);
            foreach (var message in Messages)
            {
                if (message.Role == CopilotRole.Assistant && message.Text.Length == 0)
                {
                    continue;
                }

                writer.WriteStartObject();
                writer.WriteString(RoleProperty, message.Role == CopilotRole.User ? UserRole : AssistantRole);
                writer.WriteStartArray(PartsProperty);
                writer.WriteStartObject();
                writer.WriteString(TypeProperty, TextPartType);
                writer.WriteString(TextProperty, message.Text);
                writer.WriteEndObject();
                writer.WriteEndArray();
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private void AppendMessage(CopilotMessageViewModel message)
    {
        Messages.Add(message);
        HasMessages = true;
        Description = string.Empty;
    }

    private void ApplyStatus(bool online, string? model)
    {
        IsOnline = online;
        ModelStatus = online
            ? string.IsNullOrWhiteSpace(model) ? AgentChatOptions.DefaultModel : model!
            : OfflineModel;

        if (!HasMessages)
        {
            Description = online ? EmptyHistoryHint : OfflineHint;
        }
    }

    private static string DescribeArguments(IDictionary<string, object?>? arguments)
    {
        if (arguments is null || arguments.Count == 0)
        {
            return string.Empty;
        }

        var pairs = arguments
            .Where(pair => pair.Value is not null)
            .Select(pair => string.Format(CultureInfo.InvariantCulture, ArgumentFormat, pair.Key, Truncate(Stringify(pair.Value), ArgumentValueMaxLength)));
        return string.Join(ArgumentSeparator, pairs);
    }

    private static string DescribeResult(object? result) => Truncate(Stringify(result), ResultMaxLength);

    private static string Stringify(object? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        if (value is string text)
        {
            return text;
        }

        try
        {
            return JsonSerializer.Serialize(value, HistoryJsonOptions);
        }
        catch (NotSupportedException)
        {
            return value.ToString() ?? string.Empty;
        }
    }

    private static string Truncate(string value, int max)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= max)
        {
            return value;
        }

        return string.Concat(value.AsSpan(0, max), "…");
    }
}

public enum CopilotRole
{
    User,
    Assistant
}

public partial class CopilotMessageViewModel : ObservableObject
{
    private const string CancelledText = "Cancelled.";

    private readonly Dictionary<string, CopilotStepViewModel> _stepsByCallId = new(StringComparer.Ordinal);
    private readonly StringBuilder _builder = new();

    [ObservableProperty]
    private string _text;

    [ObservableProperty]
    private bool _isStreaming;

    public CopilotMessageViewModel(CopilotRole role, string text)
    {
        Role = role;
        _text = text;
        _builder.Append(text);
        IsStreaming = role == CopilotRole.Assistant && text.Length == 0;
    }

    public CopilotRole Role { get; }

    public bool IsUser => Role == CopilotRole.User;

    public bool IsAssistant => Role == CopilotRole.Assistant;

    public ObservableCollection<CopilotStepViewModel> Steps { get; } = new();

    public bool HasSteps => Steps.Count > 0;

    public void AppendText(string delta)
    {
        if (string.IsNullOrEmpty(delta))
        {
            return;
        }

        _builder.Append(delta);
        Text = _builder.ToString();
    }

    public CopilotStepViewModel AddStep(string callId, string name, string arguments)
    {
        var step = new CopilotStepViewModel(name, arguments);
        if (!string.IsNullOrEmpty(callId))
        {
            _stepsByCallId[callId] = step;
        }

        Steps.Add(step);
        OnPropertyChanged(nameof(HasSteps));
        return step;
    }

    public void CompleteStep(string callId, string result)
    {
        if (!string.IsNullOrEmpty(callId) && _stepsByCallId.TryGetValue(callId, out var step))
        {
            step.MarkComplete(result);
        }
    }

    public void MarkCancelled()
    {
        if (_builder.Length == 0)
        {
            AppendText(CancelledText);
        }
    }

    public void Complete() => IsStreaming = false;
}

public partial class CopilotStepViewModel : ObservableObject
{
    private const string Pending = "Running";
    private const string Done = "Done";
    private const string DefaultToolName = "tool";
    private const char NameWordSeparator = '_';
    private const char DisplayWordSeparator = ' ';

    [ObservableProperty]
    private string _statusText = Pending;

    [ObservableProperty]
    private bool _isComplete;

    [ObservableProperty]
    private string _result = string.Empty;

    [ObservableProperty]
    private bool _hasResult;

    public CopilotStepViewModel(string name, string arguments)
    {
        Name = string.IsNullOrWhiteSpace(name) ? DefaultToolName : name;
        Arguments = arguments ?? string.Empty;
        DisplayName = Name.Replace(NameWordSeparator, DisplayWordSeparator);
    }

    public string Name { get; }

    public string DisplayName { get; }

    public string Arguments { get; }

    public bool HasArguments => Arguments.Length > 0;

    public void MarkComplete(string result)
    {
        Result = result ?? string.Empty;
        HasResult = Result.Length > 0;
        IsComplete = true;
        StatusText = Done;
    }
}
