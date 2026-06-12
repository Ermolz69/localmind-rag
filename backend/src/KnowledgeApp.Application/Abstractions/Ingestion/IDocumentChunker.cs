namespace KnowledgeApp.Application.Abstractions.Ingestion;

public interface IDocumentChunker
{
    IReadOnlyList<DocumentChunkText> SplitDetailed(string text);
}
