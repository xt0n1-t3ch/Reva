using Reva.Core.Contracts;

namespace Reva.Infrastructure.Ocr;

public sealed record OcrLine(
    string Text,
    double Confidence,
    int Page = 1,
    SourceBox? Bbox = null,
    IReadOnlyList<SourcePoint>? Polygon = null);

public sealed record OcrResult(string Text, IReadOnlyList<OcrLine> Lines, double AverageConfidence, int Width = 0, int Height = 0)
{
    public static OcrResult Empty { get; } = new(string.Empty, [], 0);
}

public interface IOcrEngine
{
    OcrResult Recognize(string imagePath, CancellationToken cancellationToken);
}
