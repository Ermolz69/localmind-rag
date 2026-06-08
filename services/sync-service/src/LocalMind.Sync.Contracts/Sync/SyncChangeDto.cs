namespace LocalMind.Sync.Contracts.Sync;

public sealed record SyncChangeDto(
    Guid Id,
    string EntityType,
    Guid EntityId,
    long Version,
    string Operation,
    string PayloadJson,
    DateTimeOffset CreatedAt);
