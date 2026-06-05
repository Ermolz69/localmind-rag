namespace LocalMind.Sync.Infrastructure.Redis;

using LocalMind.Sync.Application.Abstractions;
using StackExchange.Redis;

public sealed class RedisIdempotencyStore : IIdempotencyStore
{
    private readonly RedisConnectionFactory connectionFactory;

    public RedisIdempotencyStore(RedisConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public async Task<bool> TryBeginAsync(string key, TimeSpan ttl, CancellationToken cancellationToken)
    {
        bool accepted = await connectionFactory.Database.StringSetAsync(key, "accepted", ttl, When.NotExists);
        cancellationToken.ThrowIfCancellationRequested();
        return accepted;
    }
}
