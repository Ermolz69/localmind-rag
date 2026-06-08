namespace LocalMind.Sync.Infrastructure.Mongo;

using MongoDB.Bson.Serialization.Attributes;

public sealed class SyncSessionDocument
{
    [BsonId]
    public Guid Id { get; set; }

    public Guid DeviceId { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Cursor { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
