namespace LocalMind.Sync.Infrastructure.Mongo;

using LocalMind.Sync.Application.Abstractions;
using LocalMind.Sync.Domain.Changes;
using MongoDB.Driver;

public sealed class MongoChangeRepository : IChangeRepository
{
    private readonly MongoSyncContext context;

    public MongoChangeRepository(MongoSyncContext context)
    {
        this.context = context;
    }

    public Task SaveManyAsync(IReadOnlyList<SyncChange> changes, CancellationToken cancellationToken)
    {
        if (changes.Count == 0)
        {
            return Task.CompletedTask;
        }

        IEnumerable<ChangeDocument> documents = changes.Select(MongoMappers.ToDocument);
        return context.SyncChanges.InsertManyAsync(documents, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<SyncChange>> PullAsync(Guid deviceId, string cursor, int limit, CancellationToken cancellationToken)
    {
        long cursorMillis = long.TryParse(cursor, out long parsed) ? parsed : 0;
        DateTimeOffset cursorTime = DateTimeOffset.FromUnixTimeMilliseconds(cursorMillis);

        List<ChangeDocument> documents = await context.SyncChanges
            .Find(item => item.DeviceId != deviceId && item.CreatedAt > cursorTime)
            .SortBy(item => item.CreatedAt)
            .Limit(limit)
            .ToListAsync(cancellationToken);

        return documents.Select(MongoMappers.ToChange).ToArray();
    }
}
