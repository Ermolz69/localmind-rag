namespace LocalMind.Sync.Application.Abstractions;

public interface IDistributedLockService
{
    Task<IAsyncDisposable?> TryAcquireAsync(string key, TimeSpan ttl, CancellationToken cancellationToken);
}
