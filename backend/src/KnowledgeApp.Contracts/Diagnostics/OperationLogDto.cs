namespace KnowledgeApp.Contracts.Diagnostics;

public record OperationLogDto(
    string Id,
    string OperationType,
    string EntityType,
    string EntityId,
    string Message,
    string MetadataJson,
    string TraceId,
    DateTime CreatedAt);
