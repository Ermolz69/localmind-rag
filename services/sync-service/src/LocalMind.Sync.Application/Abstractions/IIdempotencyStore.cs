namespace LocalMind.Sync.Application.Abstractions;

public interface IIdempotencyStore
{
    Task<bool> TryBeginAsync(string key, TimeSpan ttl, CancellationToken cancellationToken);
}
