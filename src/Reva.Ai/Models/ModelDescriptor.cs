namespace Reva.Ai.Models;

public sealed record ModelDescriptor(string Id, string DisplayName, string Kind, string Notes, bool Installed);

public static class ModelKinds
{
    public const string Vision = "vision";
    public const string Text = "text";
    public const string Ocr = "ocr";
    public const string VisionOcr = "vision/ocr";
}

public static class CuratedModels
{
    public static IReadOnlyList<ModelDescriptor> Menu { get; } = new[]
    {
        new ModelDescriptor("qwen3.5", "Qwen 3.5", ModelKinds.Vision, "Newest Qwen, multimodal — recommended if pulled", false),
        new ModelDescriptor("qwen3-vl", "Qwen3-VL", ModelKinds.Vision, "Strong document VLM", false),
        new ModelDescriptor("qwen3-vl:8b", "Qwen3-VL 8B", ModelKinds.Vision, "Balanced local VLM", false),
        new ModelDescriptor("granite-docling", "Granite Docling", ModelKinds.VisionOcr, "IBM tiny doc VLM", false),
        new ModelDescriptor("llama4", "Llama 4", ModelKinds.Text, string.Empty, false),
        new ModelDescriptor("gemma4", "Gemma 4", ModelKinds.Text, string.Empty, false),
    };
}
