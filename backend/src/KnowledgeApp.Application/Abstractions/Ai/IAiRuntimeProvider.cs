using KnowledgeApp.Contracts.Runtime;

namespace KnowledgeApp.Application.Abstractions;

public interface IAiRuntimeProvider
{
    string ProviderId { get; }

    string ProviderName { get; }

    string EmbeddingModelName { get; }

    AiRuntimeProviderCapabilities Capabilities { get; }

    Task<RuntimeStatusDto> GetStatusAsync(CancellationToken cancellationToken = default);

    Task StartAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<string>> ListModelsAsync(CancellationToken cancellationToken = default);

    Task<string> GenerateChatCompletionAsync(ChatModelRequest request, CancellationToken cancellationToken = default);

    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
}

public interface IAiRuntimeProviderRegistry
{
    IReadOnlyCollection<IAiRuntimeProvider> Providers { get; }

    IAiRuntimeProvider GetSelectedProvider();
}
