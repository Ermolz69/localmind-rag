using KnowledgeApp.Application.Abstractions;

namespace KnowledgeApp.Infrastructure.Services;

public sealed class ProviderChatModelClient(IAiRuntimeProviderRegistry registry) : IChatModelClient
{
    public Task<string> GenerateAsync(ChatModelRequest request, CancellationToken cancellationToken = default)
    {
        return registry.GetSelectedProvider().GenerateChatCompletionAsync(request, cancellationToken);
    }

    public IAsyncEnumerable<string> GenerateStreamAsync(ChatModelRequest request, CancellationToken cancellationToken = default)
    {
        return registry.GetSelectedProvider().GenerateChatCompletionStreamAsync(request, cancellationToken);
    }
}
