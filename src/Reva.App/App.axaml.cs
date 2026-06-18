using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reva.App.Composition;
using Reva.App.ViewModels;
using Reva.App.Views;
using Reva.Infrastructure;
using Reva.Infrastructure.Agent;
using Reva.Infrastructure.Persistence;
using Reva.Infrastructure.Persistence.DemoData;

namespace Reva.App;

public partial class App : Application
{
    public IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var services = AppServices.Build();
            Services = services;

            MigrateDatabase(services);

            var shell = services.GetRequiredService<ShellViewModel>();
            desktop.MainWindow = new MainWindow
            {
                DataContext = shell
            };

            _ = Dispatcher.UIThread.InvokeAsync(() => shell.RefreshStatusAsync());
            SeedDemoDataInBackground(services);

            desktop.ShutdownRequested += (_, _) =>
            {
                if (Services is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void MigrateDatabase(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RevaDbContext>();
        dbContext.Database.Migrate();
    }

    private static void SeedDemoDataInBackground(IServiceProvider services)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = services.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<RevaDbContext>();
                var workflow = scope.ServiceProvider.GetRequiredService<IDocumentWorkflow>();
                await DemoDocumentSeeder.SeedIfEmptyAsync(workflow, dbContext, CancellationToken.None);
                services.GetRequiredService<IAppActionBus>().Publish(new AppAction(AppActionKind.Refresh));
            }
            catch (Exception)
            {
            }
        });
    }
}
