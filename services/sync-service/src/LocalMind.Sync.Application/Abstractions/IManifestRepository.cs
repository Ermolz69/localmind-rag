namespace LocalMind.Sync.Application.Abstractions;

using LocalMind.Sync.Domain.Manifests;

public interface IManifestRepository
{
    Task SaveAsync(SyncManifest manifest, CancellationToken cancellationToken);

    Task<SyncManifest?> FindLatestByDeviceIdAsync(Guid deviceId, CancellationToken cancellationToken);
}
