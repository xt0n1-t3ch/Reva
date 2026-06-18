using System;
using Microsoft.Extensions.DependencyInjection;
using Reva.App.Services;
using Reva.App.ViewModels;

namespace Reva.App.Onboarding;

public static class TourServiceLocator
{
    private static readonly object SyncRoot = new();
    private static IServiceProvider? _boundProvider;
    private static ITourService? _service;

    public static ITourService? Resolve(IServiceProvider? provider)
    {
        if (provider is null)
        {
            return null;
        }

        lock (SyncRoot)
        {
            if (_service is not null && ReferenceEquals(_boundProvider, provider))
            {
                return _service;
            }

            var registered = provider.GetService<ITourService>();
            if (registered is not null)
            {
                _boundProvider = provider;
                _service = registered;
                return registered;
            }

            var navigation = provider.GetService<INavigationService>();
            if (navigation is null)
            {
                return null;
            }

            var stateStore = provider.GetService<ITourStateStore>() ?? new TourStateStore();
            _service = new TourService(navigation, stateStore, () => provider.GetService<ShellViewModel>());
            _boundProvider = provider;
            return _service;
        }
    }
}
