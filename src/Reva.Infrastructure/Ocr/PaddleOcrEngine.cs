using OpenCvSharp;
using Sdcb.PaddleInference;
using Sdcb.PaddleOCR;
using Sdcb.PaddleOCR.Models;
using Sdcb.PaddleOCR.Models.Local;

namespace Reva.Infrastructure.Ocr;

// PaddleOCR (PP-OCR) running natively in .NET — no Python. Models ship bundled in the
// Sdcb.PaddleOCR.Models.Local package, so it works fully offline.
//
// The engine is created LAZILY on the first OCR call: the native runtime and models are
// only loaded when an image is actually parsed, so app startup stays fast and dependency-free.
// PaddleOcrAll is not thread-safe, so calls are serialized behind a lock.
public sealed class PaddleOcrEngine : IOcrEngine, IDisposable
{
    private readonly Lock _gate = new();
    private PaddleOcrAll? _engine;

    public OcrResult Recognize(string imagePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            _engine ??= CreateEngine();
            using var image = Cv2.ImRead(imagePath, ImreadModes.Color);
            if (image.Empty())
            {
                return OcrResult.Empty;
            }

            var result = _engine.Run(image);
            var lines = result.Regions
                .Select(region => new OcrLine(region.Text, region.Score))
                .ToList();
            var average = lines.Count == 0 ? 0 : lines.Average(line => line.Confidence);
            return new OcrResult(result.Text, lines, average);
        }
    }

    private static PaddleOcrAll CreateEngine()
    {
        FullOcrModel model = LocalFullModels.EnglishV5;
        // Reinsurance documents are upright pages, so rotated-text detection is left off:
        // it keeps horizontal lines clean and avoids false rotations. 180-degree classification
        // needs a model that isn't in the offline package, so it stays off too.
        return new PaddleOcrAll(model, PaddleDevice.Mkldnn())
        {
            AllowRotateDetection = false,
            Enable180Classification = false,
        };
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _engine?.Dispose();
            _engine = null;
        }
    }
}
