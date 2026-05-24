namespace KnowledgeApp.Domain.Enums;

public enum IngestionJobStatus
{
    Pending,
    Processing,
    Chunking,
    Embedding,
    Indexed,
    Failed,
    Cancelled,
}
