using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Reva.Core.Contracts;
using Reva.Core.Documents;
using Reva.Core.Reinsurance;

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
            var root = FindRepositoryRoot();
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

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Reva.slnx")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Repository root containing Reva.slnx was not found.");
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
        var content = new ByteArrayContent(File.ReadAllBytes(SamplePath("technical-account-statement.txt")));
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
    public async Task CsvBordereauUploadExtractsTableWithoutPythonRuntime()
    {
        using var client = factory.CreateClient();
        using var multipart = new MultipartFormDataContent();
        var content = new ByteArrayContent(File.ReadAllBytes(SamplePath("bordereau.csv")));
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
    public async Task UnsupportedPdfUploadIsQuarantinedAndCannotExport()
    {
        using var client = factory.CreateClient();
        using var multipart = new MultipartFormDataContent();
        var bytes = Encoding.Latin1.GetBytes("%PDF-1.4\n1 0 obj\nTitle (Antonio Martinez Resume)\nSkills C# SQL React\nExperience Programmer Analyst\nEducation Computer Science\nendobj\n%%EOF");
        multipart.Add(new ByteArrayContent(bytes), "file", "Antonio_Martinez_2026_Resume.pdf");

        var uploadResponse = await client.PostAsync("/api/documents/", multipart);
        uploadResponse.EnsureSuccessStatusCode();
        var upload = await uploadResponse.Content.ReadFromJsonAsync<DocumentUploadResult>(SerializerOptions);
        Assert.NotNull(upload);
        Assert.Equal(DocumentStatus.Unsupported, upload.Status);

        var detail = await client.GetFromJsonAsync<DocumentDetail>($"/api/documents/{upload.Id}", SerializerOptions);
        Assert.NotNull(detail);
        Assert.Equal(DocumentStatus.Unsupported, detail.Status);
        Assert.Equal(ReinsuranceDocumentType.Unknown, detail.DocumentType);
        Assert.Empty(detail.Fields);
        Assert.Contains(detail.Exceptions, issue => issue.Message.StartsWith("Unsupported document:", StringComparison.Ordinal));
        Assert.DoesNotContain(detail.Exceptions, issue => issue.Message.StartsWith("Missing canonical field:", StringComparison.Ordinal));

        var exportResponse = await client.GetAsync($"/api/documents/{upload.Id}/export");
        Assert.Equal(System.Net.HttpStatusCode.Conflict, exportResponse.StatusCode);
    }

    private static string SamplePath(string name)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "samples", name);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new FileNotFoundException($"Sample file was not found: {name}.");
    }

    private sealed record HealthPayload(string Status, string Service);
}
