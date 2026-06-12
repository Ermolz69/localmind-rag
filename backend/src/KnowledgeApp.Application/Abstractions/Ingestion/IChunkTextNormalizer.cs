namespace KnowledgeApp.Application.Abstractions.Ingestion;

public interface IChunkTextNormalizer
{
    string NormalizeForEmbedding(string text);

    string NormalizeForIdentity(string text);
}
