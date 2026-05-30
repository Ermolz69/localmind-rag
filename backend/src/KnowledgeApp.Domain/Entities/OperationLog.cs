namespace KnowledgeApp.Domain.Entities;

public class OperationLog
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string OperationType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string MetadataJson { get; set; } = "{}";
    public string TraceId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
