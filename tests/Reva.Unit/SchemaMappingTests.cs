using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Reva.Core.Contracts;
using Reva.Core.Reinsurance;
using Reva.Infrastructure.Parsing;
using Reva.Infrastructure.Persistence;
using Reva.Infrastructure.SchemaMapping;

namespace Reva.Unit;

public sealed class SchemaMappingTests
{
    [Fact]
    public async Task StaticAliasesMapSenderHeadersToCanonicalFieldsAndNormalizeValues()
    {
        await using var context = await CreateContextAsync();
        var service = new SchemaMappingService(context);
        var table = Table(["Cedant Co", "GWP", "CCY"], ["Orion Specialty", "1234", "US Dollars"]);
        var parsed = new ParsedDocument("test", "csv", "From: bordereaux@orion.example", "", "{}", [table], []);

        var result = await service.MapAsync(parsed, EmptyFields(), CancellationToken.None);

        Assert.Contains(result.Mappings, mapping => mapping.SourceHeader == "Cedant Co" && mapping.CanonicalField == ReinsuranceFieldNames.Cedent);
        Assert.Contains(result.Mappings, mapping => mapping.SourceHeader == "GWP" && mapping.CanonicalField == ReinsuranceFieldNames.Premium);
        Assert.Contains(result.Mappings, mapping => mapping.SourceHeader == "CCY" && mapping.CanonicalField == ReinsuranceFieldNames.Currency && mapping.NormalizedValue == "USD");
        Assert.Contains(result.Fields, field => field.Name == ReinsuranceFieldNames.Premium && field.Value == "USD 1,234");
    }

    [Fact]
    public async Task LearnedSenderOverrideBeatsStaticAliases()
    {
        await using var context = await CreateContextAsync();
        context.LearnedSchemaMappings.Add(new LearnedSchemaMappingRecord
        {
            SenderKey = "orion.example",
            SourceHeader = "GWP",
            NormalizedSourceHeader = SchemaMappingService.NormalizeHeader("GWP"),
            CanonicalField = ReinsuranceFieldNames.Claims,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            UseCount = 1
        });
        await context.SaveChangesAsync();
        var service = new SchemaMappingService(context);
        var table = Table(["GWP"], ["450"]);
        var parsed = new ParsedDocument("test", "csv", "From: bordereaux@orion.example", "", "{}", [table], []);

        var result = await service.MapAsync(parsed, EmptyFields(), CancellationToken.None);

        var mapping = Assert.Single(result.Mappings);
        Assert.Equal(ReinsuranceFieldNames.Claims, mapping.CanonicalField);
        Assert.Equal("learned", mapping.Source);
        Assert.True(mapping.IsLearned);
        Assert.Contains(result.Fields, field => field.Name == ReinsuranceFieldNames.Claims && field.Value == "USD 450");
    }

    [Fact]
    public async Task UnknownHeadersStayReviewableAsLowConfidenceUnmappedRows()
    {
        await using var context = await CreateContextAsync();
        var service = new SchemaMappingService(context);
        var table = Table(["🚧 carrier weird ∆"], ["abc"]);
        var parsed = new ParsedDocument("test", "csv", "no sender", "", "{}", [table], []);

        var result = await service.MapAsync(parsed, EmptyFields(), CancellationToken.None);

        var mapping = Assert.Single(result.Mappings);
        Assert.Equal(string.Empty, mapping.CanonicalField);
        Assert.Equal("unmapped", mapping.Source);
        Assert.True(mapping.Confidence < 0.3);
        Assert.Equal(SchemaMappingService.UnknownSender, mapping.SenderKey);
    }

    private static List<ExtractedField> EmptyFields() =>
        ReinsuranceFieldNames.Canonical.Select(field => new ExtractedField(field, string.Empty, 0.12, "missing", false)).ToList();

    private static ExtractedTable Table(IReadOnlyList<string> headers, IReadOnlyList<string> values)
    {
        var row = headers.Select((header, index) => (header, value: index < values.Count ? values[index] : string.Empty))
            .ToDictionary(item => item.header, item => item.value, StringComparer.OrdinalIgnoreCase);
        return new ExtractedTable("bordereau", headers, [row]);
    }

    private static async Task<RevaDbContext> CreateContextAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var context = new RevaDbContext(new DbContextOptionsBuilder<RevaDbContext>().UseSqlite(connection).Options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }
}
