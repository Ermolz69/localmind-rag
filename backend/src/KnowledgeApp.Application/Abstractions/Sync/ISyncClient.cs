namespace KnowledgeApp.Application.Abstractions;

public interface ISyncClient
{
    Task PushAsync(CancellationToken cancellationToken = default);

    Task PullAsync(CancellationToken cancellationToken = default);
}
