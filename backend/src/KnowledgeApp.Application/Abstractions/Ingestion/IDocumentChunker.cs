namespace KnowledgeApp.Application.Abstractions;

public interface IDocumentChunker
{
    IReadOnlyList<string> Split(string text);

    IReadOnlyList<DocumentChunkText> SplitDetailed(string text)
    {
        return Split(text)
            .Select(chunk => new DocumentChunkText(chunk))
            .ToArray();
    }
}
