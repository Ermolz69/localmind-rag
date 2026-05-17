namespace KnowledgeApp.Infrastructure.Options;

public sealed class OcrOptions
{
    public bool Enabled { get; init; } = true;

    public string EnginePath { get; init; } = "runtime/ocr/bin/tesseract.exe";

    public string TessDataPath { get; init; } = "runtime/ocr/tessdata";

    public string[] Languages { get; init; } = ["eng"];

    public bool DetectLanguage { get; init; } = true;

    public int MinimumImageWidth { get; init; } = 80;

    public int MinimumImageHeight { get; init; } = 30;
}
