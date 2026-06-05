namespace LocalMind.Sync.Infrastructure.Mongo;

using LocalMind.Sync.Domain.Changes;
using LocalMind.Sync.Domain.Conflicts;
using LocalMind.Sync.Domain.Devices;
using LocalMind.Sync.Domain.Manifests;
using LocalMind.Sync.Domain.Sessions;

internal static class MongoMappers
{
    public static DeviceDocument ToDocument(Device device)
    {
        return new DeviceDocument
        {
            Id = device.Id,
            Name = device.Name,
            Platform = device.Platform.ToString(),
            ClientVersion = device.ClientVersion,
            PublicKey = device.PublicKey,
            LastSeenAt = device.LastSeenAt,
            CreatedAt = device.CreatedAt,
            UpdatedAt = device.UpdatedAt,
        };
    }

    public static Device ToDevice(DeviceDocument document)
    {
        Enum.TryParse(document.Platform, out DevicePlatform platform);
        return Device.Restore(document.Id, document.Name, platform, document.ClientVersion, document.PublicKey, document.LastSeenAt, document.CreatedAt, document.UpdatedAt);
    }

    public static SyncSessionDocument ToDocument(SyncSession session)
    {
        return new SyncSessionDocument
        {
            Id = session.Id,
            DeviceId = session.DeviceId,
            Status = session.Status.ToString(),
            Cursor = session.Cursor,
            ExpiresAt = session.ExpiresAt,
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt,
        };
    }

    public static SyncSession ToSession(SyncSessionDocument document)
    {
        Enum.TryParse(document.Status, out SyncSessionStatus status);
        return SyncSession.Restore(document.Id, document.DeviceId, status, document.Cursor, document.ExpiresAt, document.CreatedAt, document.UpdatedAt);
    }

    public static ManifestDocument ToDocument(SyncManifest manifest)
    {
        return new ManifestDocument
        {
            DeviceId = manifest.DeviceId,
            CreatedAt = manifest.CreatedAt,
            Items = manifest.Items.Select(ToDocument).ToArray(),
        };
    }

    public static SyncManifest ToManifest(ManifestDocument document)
    {
        return new SyncManifest(document.DeviceId, document.Items.Select(ToManifestItem).ToArray(), document.CreatedAt);
    }

    public static ChangeDocument ToDocument(SyncChange change)
    {
        return new ChangeDocument
        {
            Id = change.Id,
            DeviceId = change.DeviceId,
            EntityType = change.EntityType,
            EntityId = change.EntityId,
            Version = change.Version,
            Operation = change.Operation.ToString(),
            PayloadJson = change.PayloadJson,
            CreatedAt = change.CreatedAt,
        };
    }

    public static SyncChange ToChange(ChangeDocument document)
    {
        Enum.TryParse(document.Operation, out SyncOperation operation);
        return new SyncChange(document.Id, document.DeviceId, document.EntityType, document.EntityId, document.Version, operation, document.PayloadJson, document.CreatedAt);
    }

    public static ConflictDocument ToDocument(SyncConflict conflict)
    {
        return new ConflictDocument
        {
            Id = conflict.Id,
            DeviceId = conflict.DeviceId,
            EntityType = conflict.EntityType,
            EntityId = conflict.EntityId,
            LocalVersion = conflict.LocalVersion,
            RemoteVersion = conflict.RemoteVersion,
            Status = conflict.Status.ToString(),
            CreatedAt = conflict.CreatedAt,
            UpdatedAt = conflict.UpdatedAt,
        };
    }

    public static SyncConflict ToConflict(ConflictDocument document)
    {
        Enum.TryParse(document.Status, out ConflictStatus status);
        return SyncConflict.Restore(document.Id, document.DeviceId, document.EntityType, document.EntityId, document.LocalVersion, document.RemoteVersion, status, document.CreatedAt, document.UpdatedAt);
    }

    private static ManifestItemDocument ToDocument(ManifestItem item)
    {
        return new ManifestItemDocument
        {
            EntityType = item.EntityType,
            EntityId = item.EntityId,
            Version = item.Version,
            Hash = item.Hash,
            UpdatedAt = item.UpdatedAt,
        };
    }

    private static ManifestItem ToManifestItem(ManifestItemDocument item)
    {
        return new ManifestItem(item.EntityType, item.EntityId, item.Version, item.Hash, item.UpdatedAt);
    }

}
