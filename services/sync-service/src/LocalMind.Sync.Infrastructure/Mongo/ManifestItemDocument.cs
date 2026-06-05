namespace LocalMind.Sync.Infrastructure.Mongo;

public sealed class ManifestItemDocument
{
    public string EntityType { get; set; } = string.Empty;

    public Guid EntityId { get; set; }

    public long Version { get; set; }

    public string Hash { get; set; } = string.Empty;

    public DateTimeOffset UpdatedAt { get; set; }
}
