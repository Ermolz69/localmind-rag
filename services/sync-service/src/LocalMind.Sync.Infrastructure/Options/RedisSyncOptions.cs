namespace LocalMind.Sync.Infrastructure.Options;

public sealed class RedisSyncOptions
{
    public const string SectionName = "Sync:Redis";

    public string ConnectionString { get; init; } = "localhost:6379";
}
