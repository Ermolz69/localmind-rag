namespace KnowledgeApp.Application.Abstractions;

public sealed record DocumentChunkText(
    string Text,
    string? HeadingPath = null,
    int? SourceStartOffset = null,
    int? SourceEndOffset = null)
{
    public string EmbeddingText => string.IsNullOrWhiteSpace(HeadingPath)
        ? Text
        : $"Section: {HeadingPath}\n\n{Text}";
}
