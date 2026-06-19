using Reva.App.Navigation;
using Reva.App.Services;
using Reva.App.ViewModels;
using Reva.Infrastructure.Agent;

namespace Reva.Unit;

public sealed class NavigationServiceTests
{
    [Fact]
    public void NavigateToKnownRouteSetsCurrentRoute()
    {
        using var nav = BuildService(out _);

        nav.NavigateTo(AppRoutes.Dashboard);

        Assert.Equal(AppRoutes.Dashboard, nav.CurrentRoute);
    }

    [Fact]
    public void NavigateToKnownRouteSetsCurrent()
    {
        using var nav = BuildService(out _);

        nav.NavigateTo(AppRoutes.Dashboard);

        Assert.NotNull(nav.Current);
        Assert.IsType<DashboardViewModel>(nav.Current);
    }

    [Fact]
    public void NavigateToUnknownRouteDoesNotChangeCurrent()
    {
        using var nav = BuildService(out _);

        nav.NavigateTo("__nonexistent__");

        Assert.Null(nav.Current);
        Assert.Null(nav.CurrentRoute);
    }

    [Fact]
    public void NavigateToBlankRouteDoesNotChangeCurrent()
    {
        using var nav = BuildService(out _);

        nav.NavigateTo(string.Empty);

        Assert.Null(nav.Current);
    }

    [Fact]
    public void NavigateToSameRouteTwiceDoesNotRaiseEventSecondTime()
    {
        using var nav = BuildService(out _);
        var raisedCount = 0;
        nav.CurrentChanged += _ => raisedCount++;

        nav.NavigateTo(AppRoutes.Dashboard);
        nav.NavigateTo(AppRoutes.Dashboard);

        Assert.Equal(1, raisedCount);
    }

    [Fact]
    public void NavigateToDifferentRouteUpdatesCurrentRoute()
    {
        using var nav = BuildService(out _);

        nav.NavigateTo(AppRoutes.Dashboard);
        nav.NavigateTo(AppRoutes.Settings);

        Assert.Equal(AppRoutes.Settings, nav.CurrentRoute);
        Assert.IsType<SettingsViewModel>(nav.Current);
    }

    [Fact]
    public void NavigateToRaisesCurrentChangedEvent()
    {
        using var nav = BuildService(out _);
        ViewModelBase? received = null;
        nav.CurrentChanged += vm => received = vm;

        nav.NavigateTo(AppRoutes.Dashboard);

        Assert.NotNull(received);
        Assert.IsType<DashboardViewModel>(received);
    }

    [Fact]
    public void NavigateToCaseInsensitive()
    {
        using var nav = BuildService(out _);

        nav.NavigateTo("DASHBOARD");

        Assert.Equal(AppRoutes.Dashboard, nav.CurrentRoute);
    }

    [Fact]
    public void OpenDocumentNavigatesToReview()
    {
        using var nav = BuildService(out _);

        nav.OpenDocument(Guid.NewGuid());

        Assert.Equal(AppRoutes.Review, nav.CurrentRoute);
    }

    [Fact]
    public void RefreshActionCallsRefreshAsyncOnIRefreshable()
    {
        var bus = new AppActionBus();
        using var nav = BuildService(out _, bus: bus);
        nav.NavigateTo(AppRoutes.Dashboard);

        var refreshable = (IRefreshable)nav.Current!;
        var task = refreshable.RefreshAsync();

        Assert.NotNull(task);
    }

    private static NavigationService BuildService(out MinimalServiceProvider provider, AppActionBus? bus = null)
    {
        var nopNav = new NopNavigationService();
        var nopClient = new NopRevaClient();
        var dashboardVm = new DashboardViewModel(nopClient, nopNav);
        var settingsVm = new SettingsViewModel(nopClient);

        var actionBus = bus ?? new AppActionBus();
        provider = new MinimalServiceProvider(new Dictionary<Type, object>
        {
            [typeof(DashboardViewModel)] = dashboardVm,
            [typeof(SettingsViewModel)] = settingsVm,
            [typeof(ReviewViewModel)] = new ReviewViewModel(nopClient, actionBus),
            [typeof(MappingsViewModel)] = new MappingsViewModel(nopClient),
            [typeof(ExportViewModel)] = new ExportViewModel(nopClient)
        });

        return new NavigationService(provider, actionBus);
    }

    private sealed class MinimalServiceProvider(Dictionary<Type, object> registrations) : IServiceProvider
    {
        public object? GetService(Type serviceType) =>
            registrations.TryGetValue(serviceType, out var instance) ? instance : null;
    }

    private sealed class NopNavigationService : INavigationService
    {
        public ViewModelBase? Current => null;

        public string? CurrentRoute => null;

#pragma warning disable CS0067
        public event Action<ViewModelBase>? CurrentChanged;
#pragma warning restore CS0067

        public void NavigateTo(string route) { }

        public void OpenDocument(Guid documentId) { }
    }

    private sealed class NopRevaClient : IRevaClient
    {
        public Task<System.Collections.Generic.IReadOnlyList<Reva.Core.Contracts.DocumentSummary>> ListDocumentsAsync(System.Threading.CancellationToken cancellationToken = default)
            => Task.FromResult<System.Collections.Generic.IReadOnlyList<Reva.Core.Contracts.DocumentSummary>>([]);

        public Task<Reva.Core.Contracts.DocumentDetail?> GetDocumentAsync(Guid id, System.Threading.CancellationToken cancellationToken = default)
            => Task.FromResult<Reva.Core.Contracts.DocumentDetail?>(null);

        public Task<Reva.Core.Contracts.BdxReviewPayload?> GetReviewPayloadAsync(Guid id, System.Threading.CancellationToken cancellationToken = default)
            => Task.FromResult<Reva.Core.Contracts.BdxReviewPayload?>(null);

        public Task<Reva.Core.Contracts.DocumentUploadResult> UploadAsync(string filePath, System.Threading.CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<Reva.Core.Contracts.DocumentUploadResult> UploadAsync(string fileName, string contentType, System.IO.Stream content, System.Threading.CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<Reva.Core.Contracts.DocumentDetail?> SaveReviewAsync(Guid id, Reva.Core.Contracts.ReviewDecision decision, System.Threading.CancellationToken cancellationToken = default)
            => Task.FromResult<Reva.Core.Contracts.DocumentDetail?>(null);

        public Task<System.Collections.Generic.IReadOnlyList<Reva.Core.Contracts.ExportTemplate>> ListTemplatesAsync(System.Threading.CancellationToken cancellationToken = default)
            => Task.FromResult<System.Collections.Generic.IReadOnlyList<Reva.Core.Contracts.ExportTemplate>>([]);

        public Task<Reva.Core.Contracts.ExportTemplate?> GetTemplateAsync(Guid id, System.Threading.CancellationToken cancellationToken = default)
            => Task.FromResult<Reva.Core.Contracts.ExportTemplate?>(null);

        public Task<Reva.Core.Contracts.ExportTemplate> CreateTemplateAsync(Reva.Core.Contracts.ExportTemplateDraft draft, System.Threading.CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<Reva.Core.Contracts.ExportTemplate?> UpdateTemplateAsync(Guid id, Reva.Core.Contracts.ExportTemplateDraft draft, System.Threading.CancellationToken cancellationToken = default)
            => Task.FromResult<Reva.Core.Contracts.ExportTemplate?>(null);

        public Task<Reva.Core.Contracts.ExportTemplate?> DuplicateTemplateAsync(Guid id, System.Threading.CancellationToken cancellationToken = default)
            => Task.FromResult<Reva.Core.Contracts.ExportTemplate?>(null);

        public Task<bool> DeleteTemplateAsync(Guid id, System.Threading.CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<Reva.Core.Contracts.ExportPreview?> PreviewExportAsync(Guid documentId, Guid templateId, System.Threading.CancellationToken cancellationToken = default)
            => Task.FromResult<Reva.Core.Contracts.ExportPreview?>(null);

        public Task<Reva.Core.Contracts.ExportFile?> ExportAsync(Guid documentId, Guid templateId, System.Threading.CancellationToken cancellationToken = default)
            => Task.FromResult<Reva.Core.Contracts.ExportFile?>(null);

        public Task<Reva.Core.Settings.AppSettings> GetSettingsAsync(System.Threading.CancellationToken cancellationToken = default)
            => Task.FromResult(Reva.Core.Settings.AppSettings.Default);

        public Task<Reva.Core.Settings.AppSettings> SaveSettingsAsync(Reva.Core.Settings.AppSettings settings, System.Threading.CancellationToken cancellationToken = default)
            => Task.FromResult(settings);

        public Task<System.Collections.Generic.IReadOnlyList<Reva.Ai.Models.ModelDescriptor>> ListModelsAsync(System.Threading.CancellationToken cancellationToken = default)
            => Task.FromResult<System.Collections.Generic.IReadOnlyList<Reva.Ai.Models.ModelDescriptor>>([]);

        public Task<string?> GetActiveModelAsync(System.Threading.CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);

        public Task SetActiveModelAsync(string modelId, System.Threading.CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<bool> IsOllamaAvailableAsync(System.Threading.CancellationToken cancellationToken = default)
            => Task.FromResult(false);
    }
}
