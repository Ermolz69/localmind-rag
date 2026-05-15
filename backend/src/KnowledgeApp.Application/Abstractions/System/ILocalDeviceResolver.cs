namespace KnowledgeApp.Application.Abstractions;

public interface ILocalDeviceResolver
{
    Task<Guid> ResolveCurrentDeviceIdAsync(CancellationToken cancellationToken = default);
}
