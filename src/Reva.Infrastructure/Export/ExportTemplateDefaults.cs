using Reva.Core.Contracts;
using Reva.Core.Export;
using Reva.Core.Reinsurance;

namespace Reva.Infrastructure.Export;

// The built-in export layouts seeded on first run. They cannot be deleted, but a user can
// duplicate one and edit the copy. Ids are fixed so seeding is idempotent.
public static class ExportTemplateDefaults
{
    public static IReadOnlyList<ExportTemplate> All =>
    [
        new ExportTemplate(
            new Guid("11111111-1111-1111-1111-111111111111"),
            "Canonical fields (CSV)",
            ExportFormat.Csv,
            [.. ReinsuranceFieldNames.Canonical.Select(name => new ExportColumn(name, name))],
            IsBuiltIn: true),

        new ExportTemplate(
            new Guid("22222222-2222-2222-2222-222222222222"),
            "Bordereau line items (Excel)",
            ExportFormat.Excel,
            [
                new ExportColumn("Cedent", ReinsuranceFieldNames.Cedent),
                new ExportColumn("Period", ReinsuranceFieldNames.Period),
                new ExportColumn("Member", "Member"),
                new ExportColumn("Line of Business", "Line of Business"),
                new ExportColumn("Premium", "Premium"),
                new ExportColumn("Claims", "Claims"),
                new ExportColumn("Commission", "Commission"),
                new ExportColumn("Net Ceded", "Net Ceded"),
                new ExportColumn("Cession %", "Cession %")
            ],
            IsBuiltIn: true),

        new ExportTemplate(
            new Guid("33333333-3333-3333-3333-333333333333"),
            "Lloyd's CRS 5.2 core (Excel)",
            ExportFormat.Excel,
            [
                new ExportColumn("Unique Market Reference", ReinsuranceFieldNames.ContractReference),
                new ExportColumn("Risk Reference", "Member"),
                new ExportColumn("Period of Cover", ReinsuranceFieldNames.Period),
                new ExportColumn("Original Currency", ReinsuranceFieldNames.Currency),
                new ExportColumn("Original Gross Premium", "Premium"),
                new ExportColumn("Commission", "Commission"),
                new ExportColumn("Cession %", "Cession %")
            ],
            IsBuiltIn: true),

        new ExportTemplate(
            new Guid("44444444-4444-4444-4444-444444444444"),
            "Full record (JSON)",
            ExportFormat.Json,
            [.. ReinsuranceFieldNames.Canonical.Select(name => new ExportColumn(name, name))],
            IsBuiltIn: true)
    ];
}
