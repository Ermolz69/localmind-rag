namespace LocalMind.Sync.Contracts.Sync;

public sealed record ManifestItemDto(
    string EntityType,
    Guid EntityId,
    long Version,
    string Hash,
    DateTimeOffset UpdatedAt);
