namespace Reva.Infrastructure;

public static partial class RevaConfigurationSections
{
    public const string Agent = "Reva:Agent";
}

public static partial class RevaConfigurationKeys
{
    public const string AgentModel = "Reva:Agent:Model";
    public const string AgentBaseUrl = "Reva:Agent:BaseUrl";
    public const string AgentNumCtx = "Reva:Agent:NumCtx";
    public const string AgentMaxSteps = "Reva:Agent:MaxSteps";
    public const string AgentTemperature = "Reva:Agent:Temperature";
}
