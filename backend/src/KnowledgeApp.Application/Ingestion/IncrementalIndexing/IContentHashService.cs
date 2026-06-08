namespace KnowledgeApp.Application.Ingestion.IncrementalIndexing;

public interface IContentHashService
{
    string ComputeChunkHash(string text);

    string ComputeDocumentHash(IEnumerable<string> orderedChunkHashes, int indexVersion);
}
