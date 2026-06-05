namespace LocalMind.Sync.Infrastructure.Options;

public sealed class MongoSyncOptions
{
    public const string SectionName = "Sync:Mongo";

    public string ConnectionString { get; init; } = "mongodb://localhost:27017";

    public string DatabaseName { get; init; } = "localmind_sync";
}
