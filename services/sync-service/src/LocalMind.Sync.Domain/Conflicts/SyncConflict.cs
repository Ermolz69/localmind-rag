namespace LocalMind.Sync.Domain.Conflicts;

using LocalMind.Sync.Domain.Common;

public sealed class SyncConflict : Entity
{
    private SyncConflict(
        Guid id,
        Guid deviceId,
        string entityType,
        Guid entityId,
        long localVersion,
        long remoteVersion,
        ConflictStatus status,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
        : base(id, createdAt, updatedAt)
    {
        DeviceId = deviceId;
        EntityType = entityType;
        EntityId = entityId;
        LocalVersion = localVersion;
        RemoteVersion = remoteVersion;
        Status = status;
    }

    public Guid DeviceId { get; }

    public string EntityType { get; }

    public Guid EntityId { get; }

    public long LocalVersion { get; }

    public long RemoteVersion { get; }

    public ConflictStatus Status { get; }

    public static SyncConflict Open(
        Guid deviceId,
        string entityType,
        Guid entityId,
        long localVersion,
        long remoteVersion,
        DateTimeOffset now)
    {
        return new SyncConflict(Guid.NewGuid(), deviceId, entityType, entityId, localVersion, remoteVersion, ConflictStatus.Open, now, now);
    }

    public static SyncConflict Restore(
        Guid id,
        Guid deviceId,
        string entityType,
        Guid entityId,
        long localVersion,
        long remoteVersion,
        ConflictStatus status,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        return new SyncConflict(id, deviceId, entityType, entityId, localVersion, remoteVersion, status, createdAt, updatedAt);
    }

    public SyncConflict Resolve(DateTimeOffset now)
    {
        return new SyncConflict(Id, DeviceId, EntityType, EntityId, LocalVersion, RemoteVersion, ConflictStatus.Resolved, CreatedAt, now);
    }
}
