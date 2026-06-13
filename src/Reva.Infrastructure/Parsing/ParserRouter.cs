using Reva.Infrastructure.Ocr;

namespace Reva.Infrastructure.Parsing;

// The single entry point for parsing. Picks the first parser that claims the file by
// extension; if none match, or a parser throws, it falls back to best-effort visible text.
// It never throws (except on cancellation) — every file yields a ParsedDocument.
public sealed class ParserRouter : IDocumentParser
{
    private readonly IReadOnlyList<IFileParser> _parsers;
    private readonly BinaryFallbackParser _fallback = new();

    public ParserRouter(IOcrEngine? ocr = null)
    {
        _parsers =
        [
            new TextFileParser(),
            new CsvFileParser(),
            new PdfFileParser(),
            new WordFileParser(),
            new PowerPointFileParser(),
            new ExcelFileParser(),
            new ImageFileParser(ocr),
            new EmailFileParser(this),
            new MsgFileParser(this),
        ];
    }

    public async Task<ParsedDocument> ParseAsync(string filePath, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        var parser = _parsers.FirstOrDefault(candidate => candidate.CanParse(extension));
        if (parser is null)
        {
            return await _fallback.ParseAsync(filePath, cancellationToken);
        }

        try
        {
            return await parser.ParseAsync(filePath, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var fallback = await _fallback.ParseAsync(filePath, cancellationToken);
            return fallback with { Warnings = [.. fallback.Warnings, $"The {parser.Profile} parser failed ({ex.Message}); used visible-text fallback."] };
        }
    }
}
