using OpenCvSharp;
using Reva.Core.Contracts;
using Sdcb.PaddleInference;
using Sdcb.PaddleOCR;
using Sdcb.PaddleOCR.Models;
using Sdcb.PaddleOCR.Models.Local;

namespace Reva.Infrastructure.Ocr;

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
                .Where(region => !string.IsNullOrWhiteSpace(region.Text))
                .Select(region => new OcrLine(region.Text, region.Score, 1, ReadBox(region, image.Width, image.Height), ReadPolygon(region, image.Width, image.Height)))
                .ToList();
            var average = lines.Count == 0 ? 0 : lines.Average(line => line.Confidence);
            return new OcrResult(result.Text, lines, average);
        }
    }

    private static SourceBox ReadBox(object region, double pageWidth, double pageHeight)
    {
        var rect = region.GetType().GetProperty("Rect")?.GetValue(region);
        if (rect is null)
        {
            return new SourceBox(0, 0, 1, 1);
        }

        var points = rect.GetType().GetProperties()
            .Select(property => property.GetValue(rect))
            .Where(value => value is not null)
            .Select(value => (X: ReadDouble(value!, "X"), Y: ReadDouble(value!, "Y")))
            .Where(point => point.X is not null && point.Y is not null)
            .Select(point => new SourcePoint(Normalize(point.X!.Value, pageWidth), Normalize(point.Y!.Value, pageHeight)))
            .ToList();
        if (points.Count == 0)
        {
            return new SourceBox(0, 0, 1, 1);
        }

        var minX = points.Min(point => point.X);
        var minY = points.Min(point => point.Y);
        var maxX = points.Max(point => point.X);
        var maxY = points.Max(point => point.Y);
        return new SourceBox(minX, minY, Math.Clamp(maxX - minX, 0, 1), Math.Clamp(maxY - minY, 0, 1));
    }

    private static List<SourcePoint> ReadPolygon(object region, double pageWidth, double pageHeight)
    {
        var rect = region.GetType().GetProperty("Rect")?.GetValue(region);
        if (rect is null)
        {
            return [];
        }

        return rect.GetType().GetProperties()
            .Select(property => property.GetValue(rect))
            .Where(value => value is not null)
            .Select(value => (X: ReadDouble(value!, "X"), Y: ReadDouble(value!, "Y")))
            .Where(point => point.X is not null && point.Y is not null)
            .Select(point => new SourcePoint(Normalize(point.X!.Value, pageWidth), Normalize(point.Y!.Value, pageHeight)))
            .ToList();
    }

    private static double? ReadDouble(object value, string name)
    {
        var property = value.GetType().GetProperty(name);
        if (property?.GetValue(value) is IConvertible convertible)
        {
            return convertible.ToDouble(System.Globalization.CultureInfo.InvariantCulture);
        }

        return null;
    }

    private static double Normalize(double value, double total) => total <= 0 ? 0 : Math.Clamp(value / total, 0, 1);

    private static PaddleOcrAll CreateEngine()
    {
        FullOcrModel model = LocalFullModels.EnglishV5;
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
