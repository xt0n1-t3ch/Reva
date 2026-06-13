namespace Reva.Infrastructure.Ocr;

// One recognized line of text with the engine's own confidence (0..1).
public sealed record OcrLine(string Text, double Confidence);

public sealed record OcrResult(string Text, IReadOnlyList<OcrLine> Lines, double AverageConfidence)
{
    public static OcrResult Empty { get; } = new(string.Empty, [], 0);
}

// Optical character recognition over a single raster image on disk.
public interface IOcrEngine
{
    OcrResult Recognize(string imagePath, CancellationToken cancellationToken);
}
