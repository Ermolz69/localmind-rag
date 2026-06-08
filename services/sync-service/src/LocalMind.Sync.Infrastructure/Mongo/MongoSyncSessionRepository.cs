namespace LocalMind.Sync.Infrastructure.Mongo;

using LocalMind.Sync.Application.Abstractions;
using LocalMind.Sync.Domain.Sessions;
using MongoDB.Driver;

public sealed class MongoSyncSessionRepository : ISyncSessionRepository
{
    private readonly MongoSyncContext context;

    public MongoSyncSessionRepository(MongoSyncContext context)
    {
        this.context = context;
    }

    public async Task<SyncSession> SaveAsync(SyncSession session, CancellationToken cancellationToken)
    {
        await context.SyncSessions.ReplaceOneAsync(
            item => item.Id == session.Id,
            MongoMappers.ToDocument(session),
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);
        return session;
    }

    public async Task<SyncSession?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        SyncSessionDocument? document = await context.SyncSessions.Find(item => item.Id == id).FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : MongoMappers.ToSession(document);
    }
}
