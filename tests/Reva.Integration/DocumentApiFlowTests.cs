using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Reva.Core.Contracts;
using Reva.Core.Documents;
using Reva.Core.Reinsurance;
using Reva.Infrastructure.Persistence;

namespace Reva.Integration;

public sealed class RevaWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"reva-{Guid.NewGuid():N}.db");
    private readonly string uploadRoot = Path.Combine(Path.GetTempPath(), $"reva-uploads-{Guid.NewGuid():N}");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration(configuration =>
        {
            var root = TestPaths.RepositoryRoot();
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Reva:Database:Provider"] = "Sqlite",
                ["Reva:Database:ConnectionString"] = $"Data Source={databasePath}",
                ["Reva:Storage:UploadRoot"] = uploadRoot,
                ["Reva:Parser:PythonExecutable"] = "python-not-available-for-reva-test",
                ["Reva:Parser:WorkerScriptPath"] = Path.Combine(root, "tools", "docling-worker", "reva_docling_worker", "main.py")
            });
        });
    }

}

public sealed class DocumentApiFlowTests : IClassFixture<RevaWebApplicationFactory>
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly RevaWebApplicationFactory factory;

    public DocumentApiFlowTests(RevaWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task HealthEndpointReturnsServiceIdentity()
    {
        using var client = factory.CreateClient();

        var health = await client.GetFromJsonAsync<HealthPayload>("/health", SerializerOptions);

        Assert.NotNull(health);
        Assert.Equal("ok", health.Status);
        Assert.Equal("Reva", health.Service);
    }

    [Fact]
    public async Task ListEndpointUsesSqliteWithoutDateTimeOffsetOrderingFailure()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/documents/");

        response.EnsureSuccessStatusCode();
        var documents = await response.Content.ReadFromJsonAsync<IReadOnlyList<DocumentSummary>>(SerializerOptions);
        Assert.NotNull(documents);
    }

    [Fact]
    public async Task UploadReviewAndExportFlowUsesRealApiAndSqlite()
    {
        using var client = factory.CreateClient();
        using var multipart = new MultipartFormDataContent();
        var content = new ByteArrayContent(File.ReadAllBytes(TestPaths.SamplePath("technical-account-statement.txt")));
        multipart.Add(content, "file", "technical-account-statement.txt");

        var uploadResponse = await client.PostAsync("/api/documents/", multipart);
        uploadResponse.EnsureSuccessStatusCode();
        var upload = await uploadResponse.Content.ReadFromJsonAsync<DocumentUploadResult>(SerializerOptions);
        Assert.NotNull(upload);
        Assert.Equal(DocumentStatus.Extracted, upload.Status);

        var documents = await client.GetFromJsonAsync<IReadOnlyList<DocumentSummary>>("/api/documents/", SerializerOptions);
        Assert.NotNull(documents);
        Assert.Contains(documents, document => document.Id == upload.Id);

        var detail = await client.GetFromJsonAsync<DocumentDetail>($"/api/documents/{upload.Id}", SerializerOptions);
        Assert.NotNull(detail);
        Assert.Equal(ReinsuranceDocumentType.StatementOfAccount, detail.DocumentType);
        Assert.Contains(detail.Fields, field => field.Name == ReinsuranceFieldNames.Cedent && field.Value == "Andes Mutual Insurance");

        var review = new ReviewDecision("Approve", "open-source-demo", "Validated sample technical account.", []);
        var reviewResponse = await client.PostAsJsonAsync($"/api/documents/{upload.Id}/review", review, SerializerOptions);
        reviewResponse.EnsureSuccessStatusCode();
        var reviewed = await reviewResponse.Content.ReadFromJsonAsync<DocumentDetail>(SerializerOptions);
        Assert.NotNull(reviewed);
        Assert.Equal(ReviewState.Approved, reviewed.ReviewState);

        var export = await client.GetFromJsonAsync<ExportRecord>($"/api/documents/{upload.Id}/export", SerializerOptions);
        Assert.NotNull(export);
        Assert.Equal(ReviewState.Approved, export.ReviewState);
        Assert.Equal("Andes Mutual Insurance", export.Fields[ReinsuranceFieldNames.Cedent]);
    }

    [Fact]
    public async Task FrontendReadEndpointsReturnDocumentReviewPayloadReconciliationAndPageImage()
    {
        using var client = factory.CreateClient();
        using var multipart = new MultipartFormDataContent();
        multipart.Add(new ByteArrayContent(File.ReadAllBytes(TestPaths.SamplePath("technical-account-statement.txt"))), "file", "frontend-read.txt");
        var upload = await (await client.PostAsync("/api/documents/", multipart))
            .Content.ReadFromJsonAsync<DocumentUploadResult>(SerializerOptions);
        Assert.NotNull(upload);

        (await client.GetAsync("/api/documents")).EnsureSuccessStatusCode();
        (await client.GetAsync($"/api/documents/{upload.Id}")).EnsureSuccessStatusCode();
        (await client.GetAsync($"/api/documents/{upload.Id}/review-payload")).EnsureSuccessStatusCode();
        (await client.GetAsync($"/api/reconciliation/{upload.Id}")).EnsureSuccessStatusCode();

        var pageDocumentId = Guid.NewGuid();
        var pagePath = await WriteTinyPngAsync(pageDocumentId);
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<RevaDbContext>();
            var now = DateTimeOffset.UtcNow;
            context.Documents.Add(new DocumentRecord
            {
                Id = pageDocumentId,
                FileName = "page-proof.png",
                Sha256Hash = Convert.ToHexString(pageDocumentId.ToByteArray()),
                Extension = ".png",
                StoragePath = pagePath,
                Status = DocumentStatus.Extracted.ToString(),
                ReviewState = ReviewState.Pending.ToString(),
                DocumentType = ReinsuranceDocumentType.Unknown.ToString(),
                Confidence = 0,
                ParsedMarkdown = string.Empty,
                ParsedJson = "{}",
                ParserProfile = "test",
                CreatedAt = now,
                UpdatedAt = now,
                Pages =
                [
                    new DocumentPageRecord
                    {
                        Page = 1,
                        ImagePath = pagePath,
                        Width = 1,
                        Height = 1,
                        Rotation = 0
                    }
                ]
            });
            await context.SaveChangesAsync();
        }

        var pageResponse = await client.GetAsync($"/api/documents/{pageDocumentId}/pages/1.png");
        pageResponse.EnsureSuccessStatusCode();
        Assert.Equal("image/png", pageResponse.Content.Headers.ContentType?.MediaType);
        File.Delete(pagePath);
    }

    [Fact]
    public async Task CsvBordereauUploadExtractsTableWithoutPythonRuntime()
    {
        using var client = factory.CreateClient();
        using var multipart = new MultipartFormDataContent();
        var content = new ByteArrayContent(File.ReadAllBytes(TestPaths.SamplePath("bordereau.csv")));
        multipart.Add(content, "file", "bordereau.csv");

        var uploadResponse = await client.PostAsync("/api/documents/", multipart);
        uploadResponse.EnsureSuccessStatusCode();
        var upload = await uploadResponse.Content.ReadFromJsonAsync<DocumentUploadResult>(SerializerOptions);
        Assert.NotNull(upload);

        var detail = await client.GetFromJsonAsync<DocumentDetail>($"/api/documents/{upload.Id}", SerializerOptions);
        Assert.NotNull(detail);
        Assert.Contains(detail.Tables, table => table.Headers.Contains("Cedent") && table.Rows.Count > 0);
    }

    [Fact]
    public async Task EmailBordereauUploadReturnsVisibleSchemaMappings()
    {
        using var client = factory.CreateClient();
        using var multipart = new MultipartFormDataContent();
        multipart.Add(new ByteArrayContent(BuildEmailBordereau("Schema variant", "Cedant Co,GWP,CCY\nOrion Specialty,1234,US Dollars\n")), "file", "schema-variant.eml");

        var uploadResponse = await client.PostAsync("/api/documents/", multipart);
        uploadResponse.EnsureSuccessStatusCode();
        var upload = await uploadResponse.Content.ReadFromJsonAsync<DocumentUploadResult>(SerializerOptions);
        Assert.NotNull(upload);

        var detail = await client.GetFromJsonAsync<DocumentDetail>($"/api/documents/{upload.Id}", SerializerOptions);
        Assert.NotNull(detail);
        Assert.Contains(detail.SchemaMappings, mapping => mapping.SourceHeader == "Cedant Co" && mapping.CanonicalField == ReinsuranceFieldNames.Cedent);
        Assert.Contains(detail.SchemaMappings, mapping => mapping.SourceHeader == "GWP" && mapping.CanonicalField == ReinsuranceFieldNames.Premium);
        Assert.Contains(detail.SchemaMappings, mapping => mapping.SourceHeader == "CCY" && mapping.CanonicalField == ReinsuranceFieldNames.Currency && mapping.NormalizedValue == "USD");
        Assert.Contains(detail.Fields, field => field.Name == ReinsuranceFieldNames.Premium && field.Value == "USD 1,234");
    }

    [Fact]
    public async Task MappingCorrectionIsLearnedForNextDocumentFromSameSender()
    {
        using var client = factory.CreateClient();
        using var firstMultipart = new MultipartFormDataContent();
        firstMultipart.Add(new ByteArrayContent(BuildEmailBordereau("Loss mapping one", "GWP,CCY\n450,USD\n", "loss.example")), "file", "loss-map-one.eml");

        var firstUpload = await (await client.PostAsync("/api/documents/", firstMultipart))
            .Content.ReadFromJsonAsync<DocumentUploadResult>(SerializerOptions);
        Assert.NotNull(firstUpload);

        var review = new ReviewDecision("RequestCorrection", "Underwriting Team", "Teach sender-specific loss header.", [])
        {
            MappingCorrections = [new SchemaMappingCorrection("GWP", ReinsuranceFieldNames.Claims)]
        };
        var reviewResponse = await client.PostAsJsonAsync($"/api/documents/{firstUpload.Id}/review", review, SerializerOptions);
        reviewResponse.EnsureSuccessStatusCode();

        using var secondMultipart = new MultipartFormDataContent();
        secondMultipart.Add(new ByteArrayContent(BuildEmailBordereau("Loss mapping two", "GWP,CCY\n550,USD\n", "loss.example")), "file", "loss-map-two.eml");
        var secondUpload = await (await client.PostAsync("/api/documents/", secondMultipart))
            .Content.ReadFromJsonAsync<DocumentUploadResult>(SerializerOptions);
        Assert.NotNull(secondUpload);

        var detail = await client.GetFromJsonAsync<DocumentDetail>($"/api/documents/{secondUpload.Id}", SerializerOptions);
        Assert.NotNull(detail);
        Assert.Contains(detail.SchemaMappings, mapping => mapping.SourceHeader == "GWP"
            && mapping.CanonicalField == ReinsuranceFieldNames.Claims
            && mapping.IsLearned);
        Assert.Contains(detail.Fields, field => field.Name == ReinsuranceFieldNames.Claims && field.Value == "USD 550");
    }

    [Fact]
    public async Task UnrecognizedFileIsIngestedBestEffortAndStaysExportable()
    {
        using var client = factory.CreateClient();
        using var multipart = new MultipartFormDataContent();
        var bytes = Encoding.Latin1.GetBytes("%PDF-1.4\n1 0 obj\nTitle (Antonio Martinez Resume)\nSkills C# SQL React\nExperience Programmer Analyst\nEducation Computer Science\nendobj\n%%EOF");
        multipart.Add(new ByteArrayContent(bytes), "file", "Antonio_Martinez_2026_Resume.pdf");

        var uploadResponse = await client.PostAsync("/api/documents/", multipart);
        uploadResponse.EnsureSuccessStatusCode();
        var upload = await uploadResponse.Content.ReadFromJsonAsync<DocumentUploadResult>(SerializerOptions);
        Assert.NotNull(upload);

        // Never hard-rejected: an unrecognized file is still ingested as a reviewable record.
        Assert.NotEqual(DocumentStatus.Failed, upload.Status);

        var detail = await client.GetFromJsonAsync<DocumentDetail>($"/api/documents/{upload.Id}", SerializerOptions);
        Assert.NotNull(detail);
        Assert.Equal(ReinsuranceDocumentType.Unknown, detail.DocumentType);
        // Best-effort, so confidence is genuinely low rather than a fabricated high score.
        Assert.True(detail.Confidence < 0.6, $"Expected low confidence for an unrecognized file, got {detail.Confidence}.");
        // It surfaces why it is uncertain instead of silently quarantining.
        Assert.Contains(detail.Exceptions, issue => issue.Message.Contains("classif", StringComparison.OrdinalIgnoreCase));

        // Raw export always works — no terminal "cannot export" state.
        var exportResponse = await client.GetAsync($"/api/documents/{upload.Id}/export");
        exportResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task CorrectedFieldComesBackMarkedReviewed()
    {
        using var client = factory.CreateClient();
        using var multipart = new MultipartFormDataContent();
        var content = new ByteArrayContent(File.ReadAllBytes(TestPaths.SamplePath("technical-account-statement.txt")));
        multipart.Add(content, "file", "technical-account-statement.txt");

        var upload = await (await client.PostAsync("/api/documents/", multipart))
            .Content.ReadFromJsonAsync<DocumentUploadResult>(SerializerOptions);
        Assert.NotNull(upload);

        var review = new ReviewDecision("Approve", "Underwriting Team", "Corrected the broker.",
            [new FieldCorrection(ReinsuranceFieldNames.Broker, "Meridian Reinsurance Brokers Ltd.")]);
        var reviewed = await (await client.PostAsJsonAsync($"/api/documents/{upload.Id}/review", review, SerializerOptions))
            .Content.ReadFromJsonAsync<DocumentDetail>(SerializerOptions);

        Assert.NotNull(reviewed);
        var broker = reviewed.Fields.Single(field => field.Name == ReinsuranceFieldNames.Broker);
        // This flag drives the "Reviewed" badge instead of an inflated AI confidence score.
        Assert.True(broker.IsCorrected);
        Assert.Equal("Meridian Reinsurance Brokers Ltd.", broker.Value);
        // A field nobody touched stays an AI extraction, not "Reviewed".
        Assert.DoesNotContain(reviewed.Fields, field => field.Name == ReinsuranceFieldNames.Cedent && field.IsCorrected);
    }

    [Theory]
    [InlineData("Approved", ReviewState.Approved)]
    [InlineData("Rejected", ReviewState.Rejected)]
    [InlineData("NeedsCorrection", ReviewState.NeedsCorrection)]
    public async Task ReviewEndpointPersistsFrontendDecisionStringsEvenWithOpenExceptions(string decision, ReviewState expected)
    {
        using var client = factory.CreateClient();
        var upload = await UploadOperationsNoteAsync(client, $"review-decision-{decision}.txt");

        var before = await client.GetFromJsonAsync<DocumentDetail>($"/api/documents/{upload.Id}", SerializerOptions);
        Assert.NotNull(before);
        Assert.NotEmpty(before.Exceptions);

        var response = await client.PostAsJsonAsync(
            $"/api/documents/{upload.Id}/review",
            new ReviewDecision(decision, "Tony", null, []),
            SerializerOptions);

        response.EnsureSuccessStatusCode();
        var reviewed = await response.Content.ReadFromJsonAsync<DocumentDetail>(SerializerOptions);
        Assert.NotNull(reviewed);
        Assert.Equal(expected, reviewed.ReviewState);
        Assert.NotEmpty(reviewed.Exceptions);

        var persisted = await client.GetFromJsonAsync<DocumentDetail>($"/api/documents/{upload.Id}", SerializerOptions);
        Assert.NotNull(persisted);
        Assert.Equal(expected, persisted.ReviewState);
        Assert.NotEmpty(persisted.Exceptions);
    }

    [Fact]
    public async Task ReviewEndpointPersistsFieldAndMappingCorrectionsOnCurrentDocument()
    {
        using var client = factory.CreateClient();
        using var multipart = new MultipartFormDataContent();
        multipart.Add(new ByteArrayContent(BuildEmailBordereau("Mapping correction persists", "GWP,CCY\n450,USD\n", "persist.example")), "file", "mapping-current.eml");

        var upload = await (await client.PostAsync("/api/documents/", multipart))
            .Content.ReadFromJsonAsync<DocumentUploadResult>(SerializerOptions);
        Assert.NotNull(upload);

        var review = new ReviewDecision(
            "Approved",
            "Tony",
            "Correct premium and teach current mapping.",
            [new FieldCorrection(ReinsuranceFieldNames.Premium, "USD 999")])
        {
            MappingCorrections = [new SchemaMappingCorrection("GWP", ReinsuranceFieldNames.Claims)]
        };
        var response = await client.PostAsJsonAsync($"/api/documents/{upload.Id}/review", review, SerializerOptions);
        response.EnsureSuccessStatusCode();

        var reviewed = await response.Content.ReadFromJsonAsync<DocumentDetail>(SerializerOptions);
        Assert.NotNull(reviewed);
        Assert.Equal(ReviewState.Approved, reviewed.ReviewState);
        Assert.Contains(reviewed.Fields, field => field.Name == ReinsuranceFieldNames.Premium && field.Value == "USD 999" && field.IsCorrected);
        Assert.Contains(reviewed.Fields, field => field.Name == ReinsuranceFieldNames.Claims && field.Value == "USD 450" && field.IsCorrected);
        Assert.Contains(reviewed.SchemaMappings, mapping => mapping.SourceHeader == "GWP"
            && mapping.CanonicalField == ReinsuranceFieldNames.Claims
            && mapping.IsCorrected
            && mapping.IsLearned);

        var persisted = await client.GetFromJsonAsync<DocumentDetail>($"/api/documents/{upload.Id}", SerializerOptions);
        Assert.NotNull(persisted);
        Assert.Equal(ReviewState.Approved, persisted.ReviewState);
        Assert.Contains(persisted.Fields, field => field.Name == ReinsuranceFieldNames.Claims && field.Value == "USD 450" && field.IsCorrected);
    }

    private static async Task<DocumentUploadResult> UploadOperationsNoteAsync(HttpClient client, string fileName)
    {
        using var multipart = new MultipartFormDataContent();
        var text = $"Internal note: process later. This is not a reinsurance document. {Guid.NewGuid():N}";
        multipart.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(text)), "file", fileName);
        var uploadResponse = await client.PostAsync("/api/documents/", multipart);
        uploadResponse.EnsureSuccessStatusCode();
        var upload = await uploadResponse.Content.ReadFromJsonAsync<DocumentUploadResult>(SerializerOptions);
        Assert.NotNull(upload);
        return upload;
    }

    private static async Task<string> WriteTinyPngAsync(Guid id)
    {
        var path = Path.Combine(Path.GetTempPath(), "reva-page-tests", $"{id:N}.png");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var bytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p9sAAAAASUVORK5CYII=");
        await File.WriteAllBytesAsync(path, bytes);
        return path;
    }

    private static byte[] BuildEmailBordereau(string subject, string csv, string senderDomain = "orion.example")
    {
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(csv));
        var message = $"""
            From: Mapping Desk <bordereaux@{senderDomain}>
            To: intake@reve.local
            Subject: {subject}
            MIME-Version: 1.0
            Content-Type: multipart/mixed; boundary="reva-boundary"

            --reva-boundary
            Content-Type: text/plain; charset=utf-8

            Please process the attached bordereau.

            --reva-boundary
            Content-Type: text/csv; name="bordereau.csv"
            Content-Disposition: attachment; filename="bordereau.csv"
            Content-Transfer-Encoding: base64

            {encoded}
            --reva-boundary--
            """;
        return Encoding.UTF8.GetBytes(message.Replace("\n", "\r\n", StringComparison.Ordinal));
    }

    private sealed record HealthPayload(string Status, string Service);
}
