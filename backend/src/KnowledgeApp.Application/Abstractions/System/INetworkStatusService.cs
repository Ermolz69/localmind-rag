namespace KnowledgeApp.Application.Abstractions;

public interface INetworkStatusService
{
    Task<bool> IsOnlineAsync(CancellationToken cancellationToken = default);
}
