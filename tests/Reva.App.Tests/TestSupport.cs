using Reva.Ai;
using Reva.App.Services;
using Reva.App.ViewModels;
using Reva.Core.Contracts;
using Reva.Core.Documents;
using Reva.Core.Reinsurance;
using Reva.Infrastructure.Agent;

namespace Reva.App.Tests;

internal sealed class TestServiceProvider(IReadOnlyDictionary<Type, object> registrations) : IServiceProvider
{
    public object? GetService(Type serviceType) =>
        registrations.TryGetValue(serviceType, out var instance) ? instance : null;
}

internal sealed class ShellHarness : IDisposable
{
    private ShellHarness(
        ShellViewModel shell,
        NavigationService navigation,
        FakeRevaClient client,
        AppActionBus actionBus)
    {
        Shell = shell;
        Navigation = navigation;
        Client = client;
        ActionBus = actionBus;
    }

    public ShellViewModel Shell { get; }

    public NavigationService Navigation { get; }

    public FakeRevaClient Client { get; }

    public AppActionBus ActionBus { get; }

    public static ShellHarness Create(FakeRevaClient? client = null, IModelRegistry? modelRegistry = null)
    {
        var revaClient = client ?? new FakeRevaClient();
        var actionBus = new AppActionBus();

        var dashboard = new DashboardViewModel(revaClient, NullNavigation.Instance);
        var review = new ReviewViewModel(revaClient, actionBus);
        var mappings = new MappingsViewModel(revaClient);
        var export = new ExportViewModel(revaClient);
        var settings = new SettingsViewModel(revaClient);

        var provider = new TestServiceProvider(new Dictionary<Type, object>
        {
            [typeof(DashboardViewModel)] = dashboard,
            [typeof(ReviewViewModel)] = review,
            [typeof(MappingsViewModel)] = mappings,
            [typeof(ExportViewModel)] = export,
            [typeof(SettingsViewModel)] = settings
        });

        var navigation = new NavigationService(provider, actionBus);
        var copilot = new CopilotViewModel(
            new SingleScopeFactory(),
            modelRegistry ?? new FakeModelRegistry(online: false),
            new FakeAgentChatService());

        var shell = new ShellViewModel(navigation, revaClient, copilot);
        return new ShellHarness(shell, navigation, revaClient, actionBus);
    }

    public void Dispose()
    {
        Shell.Dispose();
        Navigation.Dispose();
    }

    private sealed class NullNavigation : INavigationService
    {
        public static readonly NullNavigation Instance = new();

        public ViewModelBase? Current => null;

        public string? CurrentRoute => null;

#pragma warning disable CS0067
        public event Action<ViewModelBase>? CurrentChanged;
#pragma warning restore CS0067

        public void NavigateTo(string route)
        {
        }

        public void OpenDocument(Guid documentId)
        {
        }
    }
}

internal sealed class SingleScopeFactory : Microsoft.Extensions.DependencyInjection.IServiceScopeFactory
{
    public Microsoft.Extensions.DependencyInjection.IServiceScope CreateScope() => new EmptyScope();

    private sealed class EmptyScope : Microsoft.Extensions.DependencyInjection.IServiceScope
    {
        public IServiceProvider ServiceProvider { get; } = new EmptyProvider();

        public void Dispose()
        {
        }

        private sealed class EmptyProvider : IServiceProvider
        {
            public object? GetService(Type serviceType) => null;
        }
    }
}

internal static class TestData
{
    public static DocumentSummary Summary(
        ReviewState reviewState = ReviewState.Pending,
        DocumentStatus status = DocumentStatus.Extracted,
        int exceptionCount = 0,
        double confidence = 0.9,
        string fileName = "test.pdf",
        DateTimeOffset? updatedAt = null) =>
        new(
            Guid.NewGuid(),
            fileName,
            status,
            reviewState,
            ReinsuranceDocumentType.FacultativeSlip,
            confidence,
            exceptionCount,
            DateTimeOffset.UtcNow.AddDays(-1),
            updatedAt ?? DateTimeOffset.UtcNow);
}
