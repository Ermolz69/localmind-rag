namespace LocalMind.Sync.Infrastructure.Mongo;

using LocalMind.Sync.Application.Abstractions;
using LocalMind.Sync.Domain.Manifests;
using MongoDB.Driver;

public sealed class MongoManifestRepository : IManifestRepository
{
    private readonly MongoSyncContext context;

    public MongoManifestRepository(MongoSyncContext context)
    {
        this.context = context;
    }

    public Task SaveAsync(SyncManifest manifest, CancellationToken cancellationToken)
    {
        return context.SyncManifests.InsertOneAsync(MongoMappers.ToDocument(manifest), cancellationToken: cancellationToken);
    }

    public async Task<SyncManifest?> FindLatestByDeviceIdAsync(Guid deviceId, CancellationToken cancellationToken)
    {
        ManifestDocument? document = await context.SyncManifests
            .Find(item => item.DeviceId == deviceId)
            .SortByDescending(item => item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return document is null ? null : MongoMappers.ToManifest(document);
    }
}
