using KnowledgeApp.Domain.Common;
using KnowledgeApp.Domain.Enums;

namespace KnowledgeApp.Domain.Entities;

public sealed class IngestionJob : Entity
{
    public Guid DocumentId { get; set; }
    public IngestionJobStatus Status { get; set; } = IngestionJobStatus.Pending;
    public int ProgressPercent { get; set; }
    public string CurrentStep { get; set; } = "Pending";
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public int RetryCount { get; set; }
    public Guid? LastOperationId { get; set; }
}
