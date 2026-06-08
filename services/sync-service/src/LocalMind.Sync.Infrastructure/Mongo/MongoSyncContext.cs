namespace LocalMind.Sync.Infrastructure.Mongo;

using LocalMind.Sync.Infrastructure.Options;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

public sealed class MongoSyncContext
{
    private readonly IMongoDatabase database;

    public MongoSyncContext(IOptions<MongoSyncOptions> options)
    {
        MongoClient client = new(options.Value.ConnectionString);
        database = client.GetDatabase(options.Value.DatabaseName);
    }

    public IMongoCollection<DeviceDocument> Devices => database.GetCollection<DeviceDocument>("devices");

    public IMongoCollection<SyncSessionDocument> SyncSessions => database.GetCollection<SyncSessionDocument>("sync_sessions");

    public IMongoCollection<ManifestDocument> SyncManifests => database.GetCollection<ManifestDocument>("sync_manifests");

    public IMongoCollection<ChangeDocument> SyncChanges => database.GetCollection<ChangeDocument>("sync_changes");

    public IMongoCollection<ConflictDocument> SyncConflicts => database.GetCollection<ConflictDocument>("sync_conflicts");
}
