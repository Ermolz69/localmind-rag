namespace KnowledgeApp.Application.Abstractions;

public interface IAppLockService
{
    Task<bool> TryAcquireAsync(string key, CancellationToken cancellationToken = default);
}
