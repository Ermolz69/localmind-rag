namespace KnowledgeApp.Domain.Common;

public abstract class Entity
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }

    public long LocalVersion { get; set; } = 1;
}
