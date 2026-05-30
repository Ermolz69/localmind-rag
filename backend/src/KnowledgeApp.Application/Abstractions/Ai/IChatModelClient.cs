namespace KnowledgeApp.Application.Abstractions;

public interface IChatModelClient
{
    Task<string> GenerateAsync(ChatModelRequest request, CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> GenerateStreamAsync(ChatModelRequest request, CancellationToken cancellationToken = default);
}
