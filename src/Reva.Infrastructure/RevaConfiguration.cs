namespace Reva.Infrastructure;

public static partial class RevaConfigurationSections
{
    public const string Storage = "Reva:Storage";
    public const string Parser = "Reva:Parser";
}

public static partial class RevaConfigurationKeys
{
    public const string DatabaseProvider = "Reva:Database:Provider";
    public const string DatabaseConnectionString = "Reva:Database:ConnectionString";
    public const string StorageUploadRoot = "Reva:Storage:UploadRoot";
    public const string ParserPythonExecutable = "Reva:Parser:PythonExecutable";
    public const string ParserWorkerScriptPath = "Reva:Parser:WorkerScriptPath";
    public const string ParserTimeoutSeconds = "Reva:Parser:TimeoutSeconds";
    public const string LlmProvider = "Reva:Llm:Provider";
    public const string LlmBaseUrl = "Reva:Llm:BaseUrl";
    public const string LlmModel = "Reva:Llm:Model";
    public const string LlmDeterministicOnly = "Reva:Llm:DeterministicOnly";
    public const string FeaturesDocling = "Features:Docling";
    public const string InboundFileEmailDirectory = "Reva:Inbound:FileEmail:Directory";
}

public static class RevaDatabaseProviders
{
    public const string Sqlite = "Sqlite";
    public const string SqlServer = "SqlServer";
    public const string DefaultSqliteConnection = "Data Source=data/reva.db";
}


