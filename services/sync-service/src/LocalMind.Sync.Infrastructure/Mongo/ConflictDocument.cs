namespace LocalMind.Sync.Infrastructure.Mongo;

using MongoDB.Bson.Serialization.Attributes;

public sealed class ConflictDocument
{
    [BsonId]
    public Guid Id { get; set; }

    public Guid DeviceId { get; set; }

    public string EntityType { get; set; } = string.Empty;

    public Guid EntityId { get; set; }

    public long LocalVersion { get; set; }

    public long RemoteVersion { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
