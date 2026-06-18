using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Reva.Ai;
using Reva.App.Services;
using Reva.App.ViewModels;
using Reva.Infrastructure;

namespace Reva.App.Composition;

public static class AppServices
{
    private const string AppSettingsFileName = "appsettings.json";

    public static IServiceProvider Build()
    {
        AppDataPaths.Ensure();

        var configuration = BuildConfiguration();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddRevaInfrastructure(configuration);
        services.AddRevaAi(configuration);

        services.AddSingleton<IRevaClient, RevaClient>();
        services.AddSingleton<INavigationService, NavigationService>();

        services.AddSingleton<ShellViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<UploadViewModel>();
        services.AddTransient<ReviewViewModel>();
        services.AddTransient<MappingsViewModel>();
        services.AddTransient<ExportViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddSingleton<CopilotViewModel>();

        return services.BuildServiceProvider();
    }

    private static IConfiguration BuildConfiguration()
    {
        var overrides = new Dictionary<string, string?>
        {
            [RevaConfigurationKeys.DatabaseProvider] = RevaDatabaseProviders.Sqlite,
            [RevaConfigurationKeys.DatabaseConnectionString] = AppDataPaths.ConnectionString,
            [RevaConfigurationKeys.StorageUploadRoot] = AppDataPaths.UploadRoot
        };

        var builder = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory);

        var appSettingsPath = Path.Combine(AppContext.BaseDirectory, AppSettingsFileName);
        if (File.Exists(appSettingsPath))
        {
            builder.AddJsonFile(AppSettingsFileName, optional: true, reloadOnChange: false);
        }

        return builder
            .AddInMemoryCollection(overrides)
            .Build();
    }
}
