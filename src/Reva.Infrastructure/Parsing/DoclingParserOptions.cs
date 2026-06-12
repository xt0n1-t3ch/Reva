namespace Reva.Infrastructure.Parsing;

public sealed class DoclingParserOptions
{
    public string PythonExecutable { get; set; } = "python";
    public string? WorkerScriptPath { get; set; }
    public int TimeoutSeconds { get; set; } = 45;
}

