namespace LocalMind.Sync.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
