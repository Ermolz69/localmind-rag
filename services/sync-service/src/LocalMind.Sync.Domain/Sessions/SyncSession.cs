namespace LocalMind.Sync.Domain.Sessions;

using LocalMind.Sync.Domain.Common;

public sealed class SyncSession : Entity
{
    private SyncSession(
        Guid id,
        Guid deviceId,
        SyncSessionStatus status,
        string cursor,
        DateTimeOffset expiresAt,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
        : base(id, createdAt, updatedAt)
    {
        DeviceId = deviceId;
        Status = status;
        Cursor = cursor;
        ExpiresAt = expiresAt;
    }

    public Guid DeviceId { get; }

    public SyncSessionStatus Status { get; }

    public string Cursor { get; }

    public DateTimeOffset ExpiresAt { get; }

    public static SyncSession Start(Guid deviceId, TimeSpan leaseDuration, DateTimeOffset now)
    {
        return new SyncSession(Guid.NewGuid(), deviceId, SyncSessionStatus.Active, string.Empty, now.Add(leaseDuration), now, now);
    }

    public static SyncSession Restore(
        Guid id,
        Guid deviceId,
        SyncSessionStatus status,
        string cursor,
        DateTimeOffset expiresAt,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        return new SyncSession(id, deviceId, status, cursor, expiresAt, createdAt, updatedAt);
    }

    public bool IsExpired(DateTimeOffset now)
    {
        return now >= ExpiresAt || Status == SyncSessionStatus.Expired;
    }
}
