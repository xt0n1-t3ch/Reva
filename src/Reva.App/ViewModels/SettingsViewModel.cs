using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Reva.Ai.Models;
using Reva.App.Services;
using Reva.Core.Contracts;
using Reva.Core.Settings;

namespace Reva.App.ViewModels;

public sealed partial class ModelOption : ObservableObject
{
    public ModelOption(ModelDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        Id = descriptor.Id;
        DisplayName = string.IsNullOrWhiteSpace(descriptor.DisplayName) ? descriptor.Id : descriptor.DisplayName;
        Kind = descriptor.Kind ?? string.Empty;
        Notes = descriptor.Notes ?? string.Empty;
        Installed = descriptor.Installed;
    }

    public string Id { get; }

    public string DisplayName { get; }

    public string Kind { get; }

    public string Notes { get; }

    public bool Installed { get; }

    public bool HasNotes => !string.IsNullOrWhiteSpace(Notes);

    public string InstalledLabel => Installed ? "Installed" : "Not pulled";
}

public sealed partial class TemplateOption : ObservableObject
{
    public TemplateOption(Guid? id, string name)
    {
        Id = id;
        Name = name;
    }

    public Guid? Id { get; }

    public string Name { get; }
}

public sealed partial class ThemeOption : ObservableObject
{
    public ThemeOption(AppTheme value, string name)
    {
        Value = value;
        Name = name;
    }

    public AppTheme Value { get; }

    public string Name { get; }
}

public partial class SettingsViewModel : ViewModelBase
{
    private const string OllamaOnlineText = "Ollama: online";
    private const string OllamaOfflineText = "Ollama: offline";
    private const string OllamaUnknownText = "Ollama: checking…";
    private const string DeterministicNote = "Deterministic processing always works without a model. A vision model only adds OCR-grade extraction on scanned pages.";
    private const string VisionUnavailableNote = "Vision extraction needs Ollama running locally. Start Ollama, then refresh to enable it.";
    private const string NoTemplateName = "None";
    private const string SavedConfirmation = "Settings saved.";
    private const string SaveFailedText = "Could not save settings. Try again.";
    private const string LoadFailedText = "Could not load settings.";
    private const string NoModelsText = "No local models were found. Reva still extracts deterministically; install Ollama and pull a vision model to enable OCR-grade reads, then refresh.";
    private const string DoclingActiveNote = "Layout-aware OCR conversion runs on PDFs and scanned pages before extraction. Requires the Docling feature in this deployment.";
    private const string DoclingInactiveNote = "Deterministic parsing handles native PDFs. Enable layout-aware OCR conversion for scanned pages where the Docling feature is deployed.";
    private const string ThemeNoteText = "Applies instantly. System follows your operating-system appearance.";
    private const string MaintenanceRoutedNote = "Reseed and clear are run by the Copilot assistant, which has confirmed, audited access to these destructive operations. Use a clear request such as \"reseed the demo data\" or \"clear the workspace\".";
    private const string ReseedTitle = "Reseed demo data";
    private const string ReseedMessage = "This replaces the current workspace with the demo dataset. Existing documents and reviews are removed.";
    private const string ClearTitle = "Clear workspace";
    private const string ClearMessage = "This permanently removes every document, review, and export from the workspace. This cannot be undone.";
    private const string ReseedActionLabel = "Reseed";
    private const string ClearActionLabel = "Clear everything";
    private const string ReseedRoutedStatus = "Open Copilot and ask it to reseed the demo data — it runs the operation with a confirmation step.";
    private const string ClearRoutedStatus = "Open Copilot and ask it to clear the workspace — it runs the operation with a confirmation step.";

    private static readonly Guid EmptyTemplateId = Guid.Empty;

    private readonly IRevaClient _client;

    private bool _isLoading;

    private MaintenanceAction _pendingAction = MaintenanceAction.None;

    private enum MaintenanceAction
    {
        None,
        Reseed,
        Clear
    }

    [ObservableProperty]
    private string _title = "Settings";

    [ObservableProperty]
    private string _description = "Customize the model, branding, confidence tiers, reconciliation tolerance, and data tools.";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _isBusy;

    [ObservableProperty]
    private string _ollamaStatusText = OllamaUnknownText;

    [ObservableProperty]
    private bool _isOllamaOnline;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsVisionToggleEnabled))]
    private bool _isOllamaResolved;

    [ObservableProperty]
    private ModelOption? _selectedModel;

    [ObservableProperty]
    private bool _useVisionExtraction;

    [ObservableProperty]
    private string _visionNote = DeterministicNote;

    [ObservableProperty]
    private ThemeOption? _selectedTheme;

    [ObservableProperty]
    private double _reconciliationTolerance;

    [ObservableProperty]
    private double _confidenceLowMax;

    [ObservableProperty]
    private double _confidenceMediumMax;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DoclingNote))]
    private bool _enableDocling;

    [ObservableProperty]
    private string _productName = string.Empty;

    [ObservableProperty]
    private TemplateOption? _selectedTemplate;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _hasStatusMessage;

    [ObservableProperty]
    private bool _isConfirmOpen;

    [ObservableProperty]
    private string _confirmTitle = string.Empty;

    [ObservableProperty]
    private string _confirmMessage = string.Empty;

    [ObservableProperty]
    private string _maintenanceNote = MaintenanceRoutedNote;

    [ObservableProperty]
    private string _confirmActionLabel = string.Empty;

    public SettingsViewModel(IRevaClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        Models = [];
        Templates = [];
        Themes =
        [
            new ThemeOption(AppTheme.Light, "Light"),
            new ThemeOption(AppTheme.Dark, "Dark"),
            new ThemeOption(AppTheme.System, "System")
        ];
        SelectedTheme = Themes[0];
    }

    public ObservableCollection<ModelOption> Models { get; }

    public ObservableCollection<TemplateOption> Templates { get; }

    public ObservableCollection<ThemeOption> Themes { get; }

    public bool IsVisionToggleEnabled => IsOllamaResolved && IsOllamaOnline && !IsBusy;

    public bool HasModels => Models.Count > 0;

    public bool HasNoModels => Models.Count == 0;

    public string NoModelsNote { get; } = NoModelsText;

    public string ThemeNote { get; } = ThemeNoteText;

    public string DoclingNote => EnableDocling ? DoclingActiveNote : DoclingInactiveNote;

    public string DeterministicAlwaysOnNote { get; } = DeterministicNote;

    public string ReconciliationToleranceLabel => ReconciliationTolerance.ToString("P2", CultureInfo.CurrentCulture);

    public string ConfidenceLowLabel => ConfidenceLowMax.ToString("P0", CultureInfo.CurrentCulture);

    public string ConfidenceMediumLabel => ConfidenceMediumMax.ToString("P0", CultureInfo.CurrentCulture);

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        _isLoading = true;
        try
        {
            await RefreshOllamaAsync(cancellationToken).ConfigureAwait(true);
            await LoadModelsAsync(cancellationToken).ConfigureAwait(true);
            await LoadTemplatesAsync(cancellationToken).ConfigureAwait(true);
            await LoadSettingsAsync(cancellationToken).ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            SetStatus(LoadFailedText);
        }
        finally
        {
            _isLoading = false;
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        await InitializeAsync(cancellationToken).ConfigureAwait(true);
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        IsBusy = true;
        try
        {
            if (SelectedModel is { } model)
            {
                await _client.SetActiveModelAsync(model.Id, cancellationToken).ConfigureAwait(true);
            }

            var settings = BuildSettings();
            var saved = await _client.SaveSettingsAsync(settings, cancellationToken).ConfigureAwait(true);
            ApplyTheme(saved.Theme);
            SetStatus(SavedConfirmation);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            SetStatus(SaveFailedText);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanSave() => !IsBusy;

    [RelayCommand]
    private void RequestReseed()
    {
        _pendingAction = MaintenanceAction.Reseed;
        OpenConfirm(ReseedTitle, ReseedMessage, ReseedActionLabel);
    }

    [RelayCommand]
    private void RequestClearWorkspace()
    {
        _pendingAction = MaintenanceAction.Clear;
        OpenConfirm(ClearTitle, ClearMessage, ClearActionLabel);
    }

    [RelayCommand]
    private void CancelConfirm()
    {
        CloseConfirm();
    }

    [RelayCommand]
    private void AcknowledgeConfirm()
    {
        var action = _pendingAction;
        CloseConfirm();
        SetStatus(action switch
        {
            MaintenanceAction.Reseed => ReseedRoutedStatus,
            MaintenanceAction.Clear => ClearRoutedStatus,
            _ => MaintenanceRoutedNote
        });
    }

    [RelayCommand]
    private void DismissStatus()
    {
        HasStatusMessage = false;
        StatusMessage = string.Empty;
    }

    partial void OnSelectedThemeChanged(ThemeOption? value)
    {
        if (value is null)
        {
            return;
        }

        if (!_isLoading)
        {
            ApplyTheme(value.Value);
        }
    }

    partial void OnUseVisionExtractionChanged(bool value)
    {
        if (value && !(IsOllamaResolved && IsOllamaOnline))
        {
            UseVisionExtraction = false;
        }
    }

    partial void OnReconciliationToleranceChanged(double value) => OnPropertyChanged(nameof(ReconciliationToleranceLabel));

    partial void OnConfidenceLowMaxChanged(double value)
    {
        if (ConfidenceMediumMax < value)
        {
            ConfidenceMediumMax = value;
        }

        OnPropertyChanged(nameof(ConfidenceLowLabel));
    }

    partial void OnConfidenceMediumMaxChanged(double value)
    {
        if (value < ConfidenceLowMax)
        {
            ConfidenceMediumMax = ConfidenceLowMax;
            return;
        }

        OnPropertyChanged(nameof(ConfidenceMediumLabel));
    }

    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(IsVisionToggleEnabled));
    }

    partial void OnIsOllamaOnlineChanged(bool value)
    {
        OnPropertyChanged(nameof(IsVisionToggleEnabled));
    }

    private async Task RefreshOllamaAsync(CancellationToken cancellationToken)
    {
        try
        {
            var online = await _client.IsOllamaAvailableAsync(cancellationToken).ConfigureAwait(true);
            IsOllamaOnline = online;
            OllamaStatusText = online ? OllamaOnlineText : OllamaOfflineText;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            IsOllamaOnline = false;
            OllamaStatusText = OllamaOfflineText;
        }
        finally
        {
            IsOllamaResolved = true;
            UpdateVisionNote();
        }
    }

    private async Task LoadModelsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<ModelDescriptor> descriptors;
        try
        {
            descriptors = await _client.ListModelsAsync(cancellationToken).ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            descriptors = [];
        }

        Models.Clear();
        foreach (var descriptor in descriptors)
        {
            Models.Add(new ModelOption(descriptor));
        }

        OnPropertyChanged(nameof(HasModels));
        OnPropertyChanged(nameof(HasNoModels));

        string? activeId;
        try
        {
            activeId = await _client.GetActiveModelAsync(cancellationToken).ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            activeId = null;
        }

        SelectedModel = ResolveActiveModel(activeId);
    }

    private ModelOption? ResolveActiveModel(string? activeId)
    {
        if (Models.Count == 0)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(activeId))
        {
            var match = Models.FirstOrDefault(model => string.Equals(model.Id, activeId, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                return match;
            }
        }

        return Models.FirstOrDefault(model => model.Installed) ?? Models[0];
    }

    private async Task LoadTemplatesAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<ExportTemplate> templates;
        try
        {
            templates = await _client.ListTemplatesAsync(cancellationToken).ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            templates = [];
        }

        Templates.Clear();
        Templates.Add(new TemplateOption(null, NoTemplateName));
        foreach (var template in templates)
        {
            Templates.Add(new TemplateOption(template.Id, template.Name));
        }
    }

    private async Task LoadSettingsAsync(CancellationToken cancellationToken)
    {
        var settings = await _client.GetSettingsAsync(cancellationToken).ConfigureAwait(true);
        ProductName = settings.ProductName ?? string.Empty;
        ReconciliationTolerance = settings.ReconciliationTolerance;
        ConfidenceLowMax = settings.ConfidenceLowMax;
        ConfidenceMediumMax = settings.ConfidenceMediumMax;
        SelectedTheme = Themes.FirstOrDefault(theme => theme.Value == settings.Theme) ?? Themes[0];
        SelectedTemplate = ResolveTemplate(settings.DefaultTemplateId);
        UseVisionExtraction = settings.UseLlmAssist && IsOllamaOnline;
    }

    private TemplateOption ResolveTemplate(Guid? templateId)
    {
        if (templateId is { } id && id != EmptyTemplateId)
        {
            var match = Templates.FirstOrDefault(template => template.Id == id);
            if (match is not null)
            {
                return match;
            }
        }

        return Templates[0];
    }

    private AppSettings BuildSettings()
    {
        var theme = SelectedTheme?.Value ?? AppTheme.Light;
        var templateId = SelectedTemplate?.Id;
        var productName = string.IsNullOrWhiteSpace(ProductName) ? AppSettings.Default.ProductName : ProductName.Trim();
        var assist = UseVisionExtraction;
        return new AppSettings(
            theme,
            AppSettings.Default.AccentColor,
            productName,
            ClampUnit(ConfidenceLowMax),
            ClampUnit(Math.Max(ConfidenceLowMax, ConfidenceMediumMax)),
            templateId,
            ClampUnit(ReconciliationTolerance),
            assist);
    }

    private static double ClampUnit(double value)
    {
        if (double.IsNaN(value))
        {
            return 0d;
        }

        return Math.Clamp(value, 0d, 1d);
    }

    private void UpdateVisionNote()
    {
        VisionNote = IsOllamaOnline ? DeterministicNote : VisionUnavailableNote;
        if (!IsOllamaOnline)
        {
            UseVisionExtraction = false;
        }
    }

    private void OpenConfirm(string title, string message, string actionLabel)
    {
        ConfirmTitle = title;
        ConfirmMessage = message;
        ConfirmActionLabel = actionLabel;
        IsConfirmOpen = true;
    }

    private void CloseConfirm()
    {
        IsConfirmOpen = false;
        ConfirmTitle = string.Empty;
        ConfirmMessage = string.Empty;
        ConfirmActionLabel = string.Empty;
        _pendingAction = MaintenanceAction.None;
    }

    private static void ApplyTheme(AppTheme theme)
    {
        if (Application.Current is not { } app)
        {
            return;
        }

        app.RequestedThemeVariant = theme switch
        {
            AppTheme.Dark => ThemeVariant.Dark,
            AppTheme.Light => ThemeVariant.Light,
            _ => ThemeVariant.Default
        };
    }

    private void SetStatus(string message)
    {
        StatusMessage = message;
        HasStatusMessage = !string.IsNullOrWhiteSpace(message);
    }
}
