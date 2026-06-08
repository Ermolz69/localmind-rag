namespace LocalMind.Sync.Infrastructure.Mongo;

using MongoDB.Bson.Serialization.Attributes;

public sealed class ManifestDocument
{
    [BsonId]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DeviceId { get; set; }

    public IReadOnlyList<ManifestItemDocument> Items { get; set; } = Array.Empty<ManifestItemDocument>();

    public DateTimeOffset CreatedAt { get; set; }
}
