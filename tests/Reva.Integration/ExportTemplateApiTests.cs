using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Reva.Core.Contracts;
using Reva.Core.Export;

namespace Reva.Integration;

public sealed class ExportTemplateApiTests(RevaWebApplicationFactory factory) : IClassFixture<RevaWebApplicationFactory>
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task BuiltInTemplatesAreSeeded()
    {
        using var client = factory.CreateClient();

        var templates = await client.GetFromJsonAsync<List<ExportTemplate>>("/api/templates", SerializerOptions);

        Assert.NotNull(templates);
        Assert.True(templates.Count >= 4, $"Expected the built-in templates, saw {templates.Count}.");
        Assert.Contains(templates, template => template.Name.Contains("Lloyd's", StringComparison.OrdinalIgnoreCase));
        Assert.All(templates.Where(t => t.IsBuiltIn), template => Assert.NotEmpty(template.Columns));
    }

    [Fact]
    public async Task UserTemplateCrudFlowWorksAndBuiltInsAreProtected()
    {
        using var client = factory.CreateClient();

        var draft = new ExportTemplateDraft("Finance summary", ExportFormat.Csv,
            [new ExportColumn("Client", "Cedent"), new ExportColumn("Premium", "Premium")]);

        var createResponse = await client.PostAsJsonAsync("/api/templates", draft, SerializerOptions);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<ExportTemplate>(SerializerOptions);
        Assert.NotNull(created);
        Assert.False(created.IsBuiltIn);

        var updated = await (await client.PutAsJsonAsync($"/api/templates/{created.Id}",
            draft with { Name = "Finance summary v2" }, SerializerOptions)).Content.ReadFromJsonAsync<ExportTemplate>(SerializerOptions);
        Assert.Equal("Finance summary v2", updated!.Name);

        var dupResponse = await client.PostAsync($"/api/templates/{created.Id}/duplicate", null);
        Assert.Equal(HttpStatusCode.Created, dupResponse.StatusCode);

        var deleteResponse = await client.DeleteAsync($"/api/templates/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Built-in templates cannot be deleted.
        var templates = await client.GetFromJsonAsync<List<ExportTemplate>>("/api/templates", SerializerOptions);
        var builtIn = templates!.First(template => template.IsBuiltIn);
        var protectedDelete = await client.DeleteAsync($"/api/templates/{builtIn.Id}");
        Assert.Equal(HttpStatusCode.NotFound, protectedDelete.StatusCode);
    }

    [Fact]
    public async Task ExportingADocumentWithATemplateReturnsAFile()
    {
        using var client = factory.CreateClient();

        using var multipart = new MultipartFormDataContent();
        multipart.Add(new ByteArrayContent(File.ReadAllBytes(SamplePath("bordereau.csv"))), "file", "bordereau.csv");
        var upload = await (await client.PostAsync("/api/documents/", multipart))
            .Content.ReadFromJsonAsync<DocumentUploadResult>(SerializerOptions);
        Assert.NotNull(upload);

        var templates = await client.GetFromJsonAsync<List<ExportTemplate>>("/api/templates", SerializerOptions);
        var csvTemplate = templates!.First(template => template.Format == ExportFormat.Csv);

        var response = await client.GetAsync($"/api/documents/{upload.Id}/export?templateId={csvTemplate.Id}");

        response.EnsureSuccessStatusCode();
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.False(string.IsNullOrWhiteSpace(body));
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
}
