using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;
using Reva.Infrastructure.Agent;
using Reva.Infrastructure.Extraction;
using Reva.Infrastructure.Hashing;
using Reva.Infrastructure.Ingestion;
using Reva.Infrastructure.Ocr;
using Reva.Infrastructure.Parsing;
using Reva.Infrastructure.Persistence;
using Reva.Infrastructure.Rendering;
using Reva.Infrastructure.Review;
using Reva.Infrastructure.SchemaMapping;
using Reva.Infrastructure.Storage;
using System.ClientModel;

namespace Reva.Infrastructure;

public static class RevaInfrastructureRegistration
{
    public static IServiceCollection AddRevaInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RevaStorageOptions>(storage => storage.UploadRoot = configuration[RevaConfigurationKeys.StorageUploadRoot] ?? storage.UploadRoot);
        services.Configure<DoclingFeatureOptions>(feature => feature.Enabled = bool.TryParse(configuration[RevaConfigurationKeys.FeaturesDocling], out var doclingEnabled) && doclingEnabled);
        services.Configure<FileEmailInboundOptions>(source => source.Directory = configuration[RevaConfigurationKeys.InboundFileEmailDirectory] ?? source.Directory);
        services.Configure<LlmExtractionOptions>(llm =>
        {
            llm.Provider = configuration[RevaConfigurationKeys.LlmProvider] ?? llm.Provider;
            llm.BaseUrl = configuration[RevaConfigurationKeys.LlmBaseUrl] ?? llm.BaseUrl;
            llm.Model = configuration[RevaConfigurationKeys.LlmModel] ?? llm.Model;
            llm.DeterministicOnly = !bool.TryParse(configuration[RevaConfigurationKeys.LlmDeterministicOnly], out var deterministicOnly) || deterministicOnly;
        });
        services.Configure<AgentChatOptions>(agent =>
        {
            var configured = BuildAgentOptions(configuration);
            agent.Model = configured.Model;
            agent.BaseUrl = configured.BaseUrl;
            agent.NumCtx = configured.NumCtx;
            agent.MaxSteps = configured.MaxSteps;
            agent.Temperature = configured.Temperature;
        });
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
        services.AddSingleton<IOcrEngine, PaddleOcrEngine>();
        services.AddSingleton<IPdfPageImageRenderer, PdfiumPageImageRenderer>();
        services.AddSingleton<IDocumentParser, ParserRouter>();
        services.AddSingleton<IDocumentParserAdapter, DefaultDocumentParserAdapter>();
        services.AddSingleton<IDocumentParserAdapter, OptionalDoclingDocumentParser>();
        services.AddSingleton<IInboundDocumentSource, FileEmailInboundDocumentSource>();
        services.AddSingleton<IInboundDocumentSource>(new DisabledOAuthInboundDocumentSource("gmail"));
        services.AddSingleton<IInboundDocumentSource>(new DisabledOAuthInboundDocumentSource("outlook"));
        services.AddSingleton<IInboundSourceRegistry, InboundSourceRegistry>();
        services.AddSingleton<IBdxReviewPayloadAssembler, BdxReviewPayloadAssembler>();
        services.AddSingleton<IReinsuranceClassifier, ReinsuranceClassifier>();
        services.AddSingleton<IReinsuranceExtractor, ReinsuranceFieldExtractor>();
        services.AddSingleton<IExtractionMerger, ExtractionMerger>();
        services.AddSingleton<IAppActionBus, AppActionBus>();
        services.AddSingleton<IAgentChatService>(provider => new AgentChatService(
            CreateAgentOllamaChatClient(configuration),
            Microsoft.Extensions.Options.Options.Create(BuildAgentOptions(configuration)),
            provider.GetRequiredService<IAppActionBus>()));
        services.AddSingleton<ILlmFieldExtractor>(provider =>
        {
            var configuredProvider = configuration[RevaConfigurationKeys.LlmProvider] ?? LlmExtractionOptions.ProviderNone;
            var deterministicOnly = !bool.TryParse(configuration[RevaConfigurationKeys.LlmDeterministicOnly], out var value) || value;
            return deterministicOnly || !string.Equals(configuredProvider, LlmExtractionOptions.ProviderOllama, StringComparison.OrdinalIgnoreCase)
                ? new DisabledLlmFieldExtractor()
                : new OllamaLlmFieldExtractor(CreateOllamaChatClient(configuration), Microsoft.Extensions.Options.Options.Create(BuildLlmOptions(configuration)));
        });
        services.AddScoped<ISchemaMappingService, SchemaMappingService>();
        services.AddScoped<IDocumentWorkflow, DocumentWorkflow>();
        services.AddScoped<Export.IExportTemplateStore, Export.ExportTemplateStore>();
        services.AddSingleton<Export.IDocumentExporter, Export.DocumentExporter>();
        services.AddScoped<Settings.ISettingsStore, Settings.SettingsStore>();
        services.AddScoped<Settings.IDataMaintenance, Settings.DataMaintenance>();

        return services;
    }

    private static LlmExtractionOptions BuildLlmOptions(IConfiguration configuration) => new()
    {
        Provider = configuration[RevaConfigurationKeys.LlmProvider] ?? LlmExtractionOptions.ProviderNone,
        BaseUrl = configuration[RevaConfigurationKeys.LlmBaseUrl] ?? LlmExtractionOptions.DefaultBaseUrl,
        Model = configuration[RevaConfigurationKeys.LlmModel] ?? LlmExtractionOptions.DefaultModel,
        DeterministicOnly = !bool.TryParse(configuration[RevaConfigurationKeys.LlmDeterministicOnly], out var deterministicOnly) || deterministicOnly
    };

    private static AgentChatOptions BuildAgentOptions(IConfiguration configuration) => new()
    {
        Model = configuration[RevaConfigurationKeys.AgentModel] ?? AgentChatOptions.DefaultModel,
        BaseUrl = configuration[RevaConfigurationKeys.AgentBaseUrl] ?? AgentChatOptions.DefaultBaseUrl,
        NumCtx = int.TryParse(configuration[RevaConfigurationKeys.AgentNumCtx], out var numCtx) ? numCtx : AgentChatOptions.DefaultNumCtx,
        MaxSteps = int.TryParse(configuration[RevaConfigurationKeys.AgentMaxSteps], out var maxSteps) ? maxSteps : AgentChatOptions.DefaultMaxSteps,
        Temperature = double.TryParse(configuration[RevaConfigurationKeys.AgentTemperature], out var temperature) ? temperature : AgentChatOptions.DefaultTemperature
    };

    private static IChatClient CreateOllamaChatClient(IConfiguration configuration)
    {
        var options = BuildLlmOptions(configuration);
        return new OpenAI.Chat.ChatClient(options.Model, new ApiKeyCredential("ollama"), new OpenAIClientOptions { Endpoint = new Uri(options.BaseUrl) }).AsIChatClient();
    }

    private static IChatClient CreateAgentOllamaChatClient(IConfiguration configuration)
    {
        var options = BuildAgentOptions(configuration);
        return new OpenAI.Chat.ChatClient(options.Model, new ApiKeyCredential("ollama"), new OpenAIClientOptions { Endpoint = new Uri(options.BaseUrl) })
            .AsIChatClient()
            .AsBuilder()
            .UseFunctionInvocation(configure: client => client.MaximumIterationsPerRequest = options.MaxSteps)
            .Build();
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
