namespace LocalMind.Sync.Infrastructure.Options;

public sealed class RabbitMqSyncOptions
{
    public const string SectionName = "Sync:RabbitMq";

    public string HostName { get; init; } = "localhost";

    public int Port { get; init; } = 5672;

    public string UserName { get; init; } = "guest";

    public string Password { get; init; } = "guest";
}
