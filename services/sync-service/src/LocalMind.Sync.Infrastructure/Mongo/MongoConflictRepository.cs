namespace LocalMind.Sync.Infrastructure.Mongo;

using LocalMind.Sync.Application.Abstractions;
using LocalMind.Sync.Domain.Conflicts;
using MongoDB.Driver;

public sealed class MongoConflictRepository : IConflictRepository
{
    private readonly MongoSyncContext context;

    public MongoConflictRepository(MongoSyncContext context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyList<SyncConflict>> ListOpenAsync(CancellationToken cancellationToken)
    {
        List<ConflictDocument> documents = await context.SyncConflicts
            .Find(item => item.Status == ConflictStatus.Open.ToString())
            .SortByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken);

        return documents.Select(MongoMappers.ToConflict).ToArray();
    }

    public async Task<SyncConflict?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        ConflictDocument? document = await context.SyncConflicts.Find(item => item.Id == id).FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : MongoMappers.ToConflict(document);
    }

    public async Task<SyncConflict> SaveAsync(SyncConflict conflict, CancellationToken cancellationToken)
    {
        await context.SyncConflicts.ReplaceOneAsync(
            item => item.Id == conflict.Id,
            MongoMappers.ToDocument(conflict),
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);
        return conflict;
    }
}
