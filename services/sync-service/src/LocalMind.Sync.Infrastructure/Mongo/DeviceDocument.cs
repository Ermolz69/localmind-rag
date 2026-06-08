namespace LocalMind.Sync.Infrastructure.Mongo;

using MongoDB.Bson.Serialization.Attributes;

public sealed class DeviceDocument
{
    [BsonId]
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Platform { get; set; } = string.Empty;

    public string ClientVersion { get; set; } = string.Empty;

    public string PublicKey { get; set; } = string.Empty;

    public DateTimeOffset LastSeenAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
