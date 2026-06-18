using System.Text.Json;

namespace Reva.Infrastructure.Parsing;

public sealed class GoogleSheetsStubParser : IFileParser
{
    private const string GuidanceText =
        "Google Sheets shortcut — open in Google Sheets and download as .xlsx, .csv, or .ods to import.";

    public string Profile => "gsheet-stub";

    public bool CanParse(string extension) => extension == ".gsheet";

    public async Task<ParsedDocument> ParseAsync(string filePath, CancellationToken cancellationToken)
    {
        var warnings = new List<string> { "Google Sheets shortcut files cannot be imported directly. Download the sheet as .xlsx, .csv, or .ods from Google Sheets first." };

        try
        {
            var raw = await File.ReadAllTextAsync(filePath, System.Text.Encoding.UTF8, cancellationToken);
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.TryGetProperty("url", out var urlProp))
            {
                var url = urlProp.GetString();
                if (!string.IsNullOrWhiteSpace(url))
                {
                    warnings.Add($"Source URL: {url}");
                }
            }
        }
        catch (JsonException)
        {
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
        }

        return ParseSupport.Build(Profile, "gsheet", GuidanceText, GuidanceText, warnings: warnings);
    }
}
