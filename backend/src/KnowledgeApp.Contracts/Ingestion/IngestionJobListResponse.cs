namespace KnowledgeApp.Contracts.Ingestion;

/// <summary>Offset-paged ingestion jobs for diagnostics and job management.</summary>
public sealed record IngestionJobListResponse(
    IReadOnlyList<IngestionJobDto> Items,
    int TotalCount,
    int Limit,
    int Offset);
