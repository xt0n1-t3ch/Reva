using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Reva.Infrastructure.Extraction;
using Reva.Infrastructure.Hashing;
using Reva.Infrastructure.Parsing;
using Reva.Infrastructure.Persistence;
using Reva.Infrastructure.Storage;

namespace Reva.Infrastructure;

public static class RevaInfrastructureRegistration
{
    public static IServiceCollection AddRevaInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RevaStorageOptions>(storage => storage.UploadRoot = configuration[RevaConfigurationKeys.StorageUploadRoot] ?? storage.UploadRoot);
        services.Configure<DoclingParserOptions>(parser =>
        {
            parser.PythonExecutable = configuration[RevaConfigurationKeys.ParserPythonExecutable] ?? parser.PythonExecutable;
            parser.WorkerScriptPath = configuration[RevaConfigurationKeys.ParserWorkerScriptPath];
            parser.TimeoutSeconds = int.TryParse(configuration[RevaConfigurationKeys.ParserTimeoutSeconds], out var timeoutSeconds) ? timeoutSeconds : parser.TimeoutSeconds;
        });

        services.AddDbContext<RevaDbContext>(options =>
        {
            var provider = configuration[RevaConfigurationKeys.DatabaseProvider] ?? RevaDatabaseProviders.Sqlite;
            var connectionString = configuration[RevaConfigurationKeys.DatabaseConnectionString] ?? RevaDatabaseProviders.DefaultSqliteConnection;

            if (string.Equals(provider, RevaDatabaseProviders.SqlServer, StringComparison.OrdinalIgnoreCase))
            {
                options.UseSqlServer(connectionString);
                return;
            }

            EnsureSqliteDirectory(connectionString);
            options.UseSqlite(connectionString);
        });

        services.AddSingleton<IDocumentHasher, Sha256DocumentHasher>();
        services.AddSingleton<IDocumentStorage, LocalDocumentStorage>();
        services.AddSingleton<IDocumentParser, ParserRouter>();
        services.AddSingleton<IReinsuranceClassifier, ReinsuranceClassifier>();
        services.AddSingleton<IReinsuranceExtractor, ReinsuranceFieldExtractor>();
        services.AddScoped<IDocumentWorkflow, DocumentWorkflow>();

        return services;
    }

    private static void EnsureSqliteDirectory(string connectionString)
    {
        var dataSource = new SqliteConnectionStringBuilder(connectionString).DataSource;
        if (string.IsNullOrWhiteSpace(dataSource) || string.Equals(dataSource, ":memory:", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(dataSource));
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}

