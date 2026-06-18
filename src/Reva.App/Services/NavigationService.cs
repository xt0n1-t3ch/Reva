using System;
using System.Collections.Generic;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Reva.App.Navigation;
using Reva.App.ViewModels;
using Reva.Infrastructure.Agent;

namespace Reva.App.Services;

public sealed class NavigationService : INavigationService, IDisposable
{
    private readonly IServiceProvider _services;
    private readonly IDisposable? _actionSubscription;
    private readonly Dictionary<string, Type> _routes = new(StringComparer.OrdinalIgnoreCase)
    {
        [AppRoutes.Dashboard] = typeof(DashboardViewModel),
        [AppRoutes.Review] = typeof(ReviewViewModel),
        [AppRoutes.Mappings] = typeof(MappingsViewModel),
        [AppRoutes.Export] = typeof(ExportViewModel),
        [AppRoutes.Settings] = typeof(SettingsViewModel)
    };

    public NavigationService(IServiceProvider services, IAppActionBus actionBus)
    {
        _services = services;
        _actionSubscription = actionBus.Actions.Subscribe(new ActionObserver(this));
    }

    public ViewModelBase? Current { get; private set; }

    public string? CurrentRoute { get; private set; }

    public event Action<ViewModelBase>? CurrentChanged;

    public void NavigateTo(string route)
    {
        if (string.IsNullOrWhiteSpace(route) || !_routes.TryGetValue(route, out var viewModelType))
        {
            return;
        }

        if (string.Equals(CurrentRoute, route, StringComparison.OrdinalIgnoreCase) && Current is not null)
        {
            return;
        }

        if (_services.GetService(viewModelType) is not ViewModelBase viewModel)
        {
            return;
        }

        Current = viewModel;
        CurrentRoute = route;
        CurrentChanged?.Invoke(viewModel);
    }

    public void OpenDocument(Guid documentId)
    {
        NavigateTo(AppRoutes.Review);
        if (Current is IDocumentNavigationTarget target)
        {
            target.RequestDocument(documentId);
        }
    }

    public void Dispose() => _actionSubscription?.Dispose();

    private void Dispatch(AppAction action)
    {
        switch (action.Kind)
        {
            case AppActionKind.Navigate when !string.IsNullOrWhiteSpace(action.Route):
                NavigateTo(action.Route!);
                break;
            case AppActionKind.OpenDocument when Guid.TryParse(action.DocumentId, out var documentId):
                OpenDocument(documentId);
                break;
            case AppActionKind.Refresh when Current is IRefreshable refreshable:
                _ = refreshable.RefreshAsync();
                break;
        }
    }

    private sealed class ActionObserver(NavigationService owner) : IObserver<AppAction>
    {
        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(AppAction value)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                owner.Dispatch(value);
                return;
            }

            Dispatcher.UIThread.Post(() => owner.Dispatch(value));
        }
    }
}
