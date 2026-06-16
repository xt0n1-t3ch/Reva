using Reva.Core.Contracts;

namespace Reva.Infrastructure.Parsing;

public sealed record ParsedPage(int Page, string ImagePath, double Width, double Height, int Rotation);

public sealed record ParsedDocument(
    string ParserProfile,
    string SourceFormat,
    string Text,
    string Markdown,
    string RawJson,
    IReadOnlyList<ExtractedTable> Tables,
    IReadOnlyList<string> Warnings)
{
    public IReadOnlyList<SourceSpan> SourceSpans { get; init; } = [];
    public IReadOnlyList<ParsedPage> Pages { get; init; } = [];
}
