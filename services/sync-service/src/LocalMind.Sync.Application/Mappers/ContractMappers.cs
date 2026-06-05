namespace LocalMind.Sync.Application.Mappers;

using LocalMind.Sync.Contracts.Conflicts;
using LocalMind.Sync.Contracts.Devices;
using LocalMind.Sync.Contracts.Sessions;
using LocalMind.Sync.Contracts.Sync;
using LocalMind.Sync.Domain.Changes;
using LocalMind.Sync.Domain.Conflicts;
using LocalMind.Sync.Domain.Devices;
using LocalMind.Sync.Domain.Manifests;
using LocalMind.Sync.Domain.Sessions;

internal static class ContractMappers
{
    public static DeviceResponse ToResponse(Device device)
    {
        return new DeviceResponse(device.Id, device.Name, device.Platform.ToString(), device.ClientVersion, device.LastSeenAt, device.CreatedAt);
    }

    public static SyncSessionResponse ToResponse(SyncSession session)
    {
        return new SyncSessionResponse(session.Id, session.DeviceId, session.Status.ToString(), session.Cursor, session.ExpiresAt, session.CreatedAt);
    }

    public static ManifestItem ToDomain(ManifestItemDto dto)
    {
        return new ManifestItem(dto.EntityType, dto.EntityId, dto.Version, dto.Hash, dto.UpdatedAt);
    }

    public static ManifestItemDto ToDto(ManifestItem item)
    {
        return new ManifestItemDto(item.EntityType, item.EntityId, item.Version, item.Hash, item.UpdatedAt);
    }

    public static SyncChange ToDomain(SyncChangeDto dto, Guid deviceId)
    {
        Enum.TryParse<SyncOperation>(dto.Operation, ignoreCase: true, out SyncOperation operation);
        return new SyncChange(dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id, deviceId, dto.EntityType, dto.EntityId, dto.Version, operation, dto.PayloadJson, dto.CreatedAt);
    }

    public static SyncChangeDto ToDto(SyncChange change)
    {
        return new SyncChangeDto(change.Id, change.EntityType, change.EntityId, change.Version, change.Operation.ToString(), change.PayloadJson, change.CreatedAt);
    }

    public static ConflictResponse ToResponse(SyncConflict conflict)
    {
        return new ConflictResponse(conflict.Id, conflict.DeviceId, conflict.EntityType, conflict.EntityId, conflict.LocalVersion, conflict.RemoteVersion, conflict.Status.ToString(), conflict.CreatedAt);
    }
}
