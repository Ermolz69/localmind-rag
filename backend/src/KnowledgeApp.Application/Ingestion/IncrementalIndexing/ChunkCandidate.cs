namespace KnowledgeApp.Application.Ingestion.IncrementalIndexing;

public sealed record ChunkCandidate(
    int Index,
    int? PageNumber,
    string Text,
    string TextHash,
    int ChunkVersion,
    string? HeadingPath = null,
    int? SourceStartOffset = null,
    int? SourceEndOffset = null);
