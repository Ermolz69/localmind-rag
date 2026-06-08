namespace LocalMind.Sync.Infrastructure.Mongo;

using MongoDB.Bson.Serialization.Attributes;

public sealed class ChangeDocument
{
    [BsonId]
    public Guid Id { get; set; }

    public Guid DeviceId { get; set; }

    public string EntityType { get; set; } = string.Empty;

    public Guid EntityId { get; set; }

    public long Version { get; set; }

    public string Operation { get; set; } = string.Empty;

    public string PayloadJson { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}
