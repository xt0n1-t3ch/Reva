namespace Reva.Infrastructure;

public static class RevaConfigurationSections
{
    public const string Storage = "Reva:Storage";
    public const string Parser = "Reva:Parser";
}

public static class RevaConfigurationKeys
{
    public const string DatabaseProvider = "Reva:Database:Provider";
    public const string DatabaseConnectionString = "Reva:Database:ConnectionString";
    public const string StorageUploadRoot = "Reva:Storage:UploadRoot";
    public const string ParserPythonExecutable = "Reva:Parser:PythonExecutable";
    public const string ParserWorkerScriptPath = "Reva:Parser:WorkerScriptPath";
    public const string ParserTimeoutSeconds = "Reva:Parser:TimeoutSeconds";
}

public static class RevaDatabaseProviders
{
    public const string Sqlite = "Sqlite";
    public const string SqlServer = "SqlServer";
    public const string DefaultSqliteConnection = "Data Source=data/reva.db";
}


