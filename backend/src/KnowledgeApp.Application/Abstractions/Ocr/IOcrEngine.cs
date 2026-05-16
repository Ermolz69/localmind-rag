namespace KnowledgeApp.Application.Abstractions;

public interface IOcrEngine
{
    Task<OcrTextResult> ExtractAsync(string imagePath, CancellationToken cancellationToken = default);
}

public sealed record OcrTextResult(string Text, string? DetectedLanguage, string? DetectedScript, double? Confidence);
