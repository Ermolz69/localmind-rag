namespace LocalMind.Sync.Domain.Changes;

public sealed record SyncChange(
    Guid Id,
    Guid DeviceId,
    string EntityType,
    Guid EntityId,
    long Version,
    SyncOperation Operation,
    string PayloadJson,
    DateTimeOffset CreatedAt);
