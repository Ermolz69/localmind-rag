namespace LocalMind.Sync.Domain.Manifests;

public sealed record ManifestItem(
    string EntityType,
    Guid EntityId,
    long Version,
    string Hash,
    DateTimeOffset UpdatedAt);
