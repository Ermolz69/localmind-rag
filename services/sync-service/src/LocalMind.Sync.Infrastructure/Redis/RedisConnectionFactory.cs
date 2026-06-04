namespace LocalMind.Sync.Infrastructure.Redis;

using LocalMind.Sync.Infrastructure.Options;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

public sealed class RedisConnectionFactory : IDisposable
{
    private readonly Lazy<ConnectionMultiplexer> connection;

    public RedisConnectionFactory(IOptions<RedisSyncOptions> options)
    {
        connection = new Lazy<ConnectionMultiplexer>(() => ConnectionMultiplexer.Connect(options.Value.ConnectionString));
    }

    public IDatabase Database => connection.Value.GetDatabase();

    public void Dispose()
    {
        if (connection.IsValueCreated)
        {
            connection.Value.Dispose();
        }
    }
}
