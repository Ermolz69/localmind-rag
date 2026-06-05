namespace LocalMind.Sync.Infrastructure.Time;

using LocalMind.Sync.Application.Abstractions;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
