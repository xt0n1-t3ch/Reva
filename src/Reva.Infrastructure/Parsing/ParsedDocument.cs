using Reva.Core.Contracts;

namespace Reva.Infrastructure.Parsing;

public sealed record ParsedDocument(
    string ParserProfile,
    string SourceFormat,
    string Text,
    string Markdown,
    string RawJson,
    IReadOnlyList<ExtractedTable> Tables,
    IReadOnlyList<string> Warnings);

