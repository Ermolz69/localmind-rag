namespace KnowledgeApp.Application.Abstractions.Sync;

public interface ILocalDeviceIdentityService
{
    Task<Guid> GetLocalDeviceIdAsync(CancellationToken cancellationToken = default);
}
