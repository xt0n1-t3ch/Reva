namespace Reva.Core.Reinsurance;

public static class ReinsuranceFieldNames
{
    public const string Cedent = "Cedent";
    public const string Broker = "Broker";
    public const string Reinsurer = "Reinsurer";
    public const string ContractReference = "Contract Reference";
    public const string LineOfBusiness = "Line of Business";
    public const string Period = "Period";
    public const string Currency = "Currency";
    public const string Premium = "Premium";
    public const string Claims = "Claims";
    public const string Commission = "Commission";
    public const string Cession = "Cession %";
    public const string Retention = "Retention";
    public const string Limit = "Limit";

    public static readonly string[] Canonical =
    [
        Cedent,
        Broker,
        Reinsurer,
        ContractReference,
        LineOfBusiness,
        Period,
        Currency,
        Premium,
        Claims,
        Commission,
        Cession,
        Retention,
        Limit
    ];
}

