using System.Text;
using System.Text.Json;
using Reva.Core.Contracts;
using Reva.Core.Documents;
using Reva.Core.Export;
using Reva.Core.Reinsurance;
using Reva.Infrastructure.Export;

namespace Reva.Unit;

public sealed class ExportTemplateTests
{
    private static readonly DocumentExporter Exporter = new();

    private static DocumentDetail SampleDocument()
    {
        IReadOnlyList<IReadOnlyDictionary<string, string>> rows =
        [
            new Dictionary<string, string> { ["Member"] = "North America", ["Premium"] = "2450000" },
            new Dictionary<string, string> { ["Member"] = "Europe", ["Premium"] = "1850000" }
        ];
        var table = new ExtractedTable("bordereau", ["Member", "Premium"], rows);
        var fields = new List<ExtractedField>
        {
            new(ReinsuranceFieldNames.Cedent, "Orion Insurance", 0.9, "label", false),
            new(ReinsuranceFieldNames.Currency, "USD", 0.99, "label", false)
        };
        return new DocumentDetail(Guid.NewGuid(), "hero.eml", "hash", DocumentStatus.Extracted, ReviewState.Pending,
            ReinsuranceDocumentType.Bordereau, 0.85, "markdown", "email-eml", fields, [table], [], DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
    }

    [Fact]
    public void PreviewUsesCustomHeadersAndOneRowPerLineItem()
    {
        var template = new ExportTemplate(Guid.NewGuid(), "Custom", ExportFormat.Csv,
            [new ExportColumn("Cedant Co", ReinsuranceFieldNames.Cedent), new ExportColumn("Risk", "Member"), new ExportColumn("GWP", "Premium")],
            false);

        var preview = Exporter.Preview(SampleDocument(), template);

        Assert.Equal(["Cedant Co", "Risk", "GWP"], preview.Headers);
        // A table column is referenced, so there is one row per line item.
        Assert.Equal(2, preview.Rows.Count);
        // Document-level field repeats; table columns pull per row.
        Assert.Equal(["Orion Insurance", "North America", "2450000"], preview.Rows[0]);
        Assert.Equal(["Orion Insurance", "Europe", "1850000"], preview.Rows[1]);
    }

    [Fact]
    public void DocumentLevelTemplateProducesASingleRow()
    {
        var template = new ExportTemplate(Guid.NewGuid(), "Doc", ExportFormat.Csv,
            [new ExportColumn("Cedent", ReinsuranceFieldNames.Cedent), new ExportColumn("Currency", ReinsuranceFieldNames.Currency)],
            false);

        var preview = Exporter.Preview(SampleDocument(), template);

        Assert.Single(preview.Rows);
        Assert.Equal(["Orion Insurance", "USD"], preview.Rows[0]);
    }

    [Fact]
    public void CsvExportContainsHeadersAndValues()
    {
        var template = new ExportTemplate(Guid.NewGuid(), "Custom", ExportFormat.Csv,
            [new ExportColumn("Risk", "Member"), new ExportColumn("GWP", "Premium")], false);

        var file = Exporter.Export(SampleDocument(), template);
        var text = Encoding.UTF8.GetString(file.Content);

        Assert.Equal("text/csv", file.ContentType);
        Assert.EndsWith(".csv", file.FileName);
        Assert.Contains("Risk,GWP", text, StringComparison.Ordinal);
        Assert.Contains("North America,2450000", text, StringComparison.Ordinal);
    }

    [Fact]
    public void JsonExportSerializesRowsAsObjectsWithCustomKeys()
    {
        var template = new ExportTemplate(Guid.NewGuid(), "Json", ExportFormat.Json,
            [new ExportColumn("Risk", "Member"), new ExportColumn("GWP", "Premium")], false);

        var file = Exporter.Export(SampleDocument(), template);

        Assert.Equal("application/json", file.ContentType);
        using var json = JsonDocument.Parse(file.Content);
        Assert.Equal(2, json.RootElement.GetArrayLength());
        Assert.Equal("North America", json.RootElement[0].GetProperty("Risk").GetString());
        Assert.Equal("2450000", json.RootElement[0].GetProperty("GWP").GetString());
    }

    [Fact]
    public void ExcelExportProducesAWorkbookFile()
    {
        var template = new ExportTemplate(Guid.NewGuid(), "Excel", ExportFormat.Excel,
            [new ExportColumn("Risk", "Member")], false);

        var file = Exporter.Export(SampleDocument(), template);

        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", file.ContentType);
        Assert.EndsWith(".xlsx", file.FileName);
        // A valid .xlsx is a ZIP (PK signature).
        Assert.True(file.Content.Length > 4 && file.Content[0] == 0x50 && file.Content[1] == 0x4B);
    }
}
