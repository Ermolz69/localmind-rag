namespace LocalMind.Sync.Infrastructure.Redis;

using LocalMind.Sync.Application.Abstractions;
using StackExchange.Redis;

public sealed class RedisDistributedLockService : IDistributedLockService
{
    private readonly RedisConnectionFactory connectionFactory;

    public RedisDistributedLockService(RedisConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task<IAsyncDisposable?> TryAcquireAsync(string key, TimeSpan ttl, CancellationToken cancellationToken)
    {
        string token = Guid.NewGuid().ToString("N");
        bool acquired = await connectionFactory.Database.StringSetAsync(key, token, ttl, When.NotExists);
        cancellationToken.ThrowIfCancellationRequested();
        return acquired ? new RedisLock(connectionFactory.Database, key, token) : null;
    }

    private sealed class RedisLock : IAsyncDisposable
    {
        private readonly IDatabase database;
        private readonly string key;
        private readonly string token;

        public RedisLock(IDatabase database, string key, string token)
        {
            this.database = database;
            this.key = key;
            this.token = token;
        }

        public async ValueTask DisposeAsync()
        {
            RedisValue current = await database.StringGetAsync(key);
            if (current == token)
            {
                await database.KeyDeleteAsync(key);
            }
        }
    }
}
