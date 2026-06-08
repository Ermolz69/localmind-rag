namespace LocalMind.Sync.Domain.Common;

public abstract class Entity
{
    protected Entity(Guid id, DateTimeOffset createdAt, DateTimeOffset updatedAt)
    {
        Id = id;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; protected init; }
}
