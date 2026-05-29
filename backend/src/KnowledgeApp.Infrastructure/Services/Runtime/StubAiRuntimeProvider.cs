using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Runtime;
using KnowledgeApp.Infrastructure.Options;

using Microsoft.Extensions.Options;

namespace KnowledgeApp.Infrastructure.Services;

public sealed class StubAiRuntimeProvider(
    StubEmbeddingGenerator embeddings,
    StubChatModelClient chat,
    IOptions<RuntimeOptions> options) : IAiRuntimeProvider
{
    private readonly RuntimeOptions options = options.Value;

    public string ProviderId => "stub";

    public string ProviderName => "Stub";

    public string EmbeddingModelName => embeddings.ModelName;

    public AiRuntimeProviderCapabilities Capabilities { get; } = new(
        SupportsEmbeddings: true,
        SupportsChat: true,
        SupportsModelListing: true,
        SupportsSetup: false,
        SupportsStart: false,
        SupportsStop: false);

    public Task<RuntimeStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new RuntimeStatusDto(
            LocalApiReady: true,
            AiRuntimeStatus: "Running",
            ModelsAvailable: true,
            OfflineMode: true,
            SetupRequired: false,
            ProviderId: ProviderId,
            ProviderName: ProviderName,
            ProviderStatus: AiRuntimeProviderStatus.Running,
            Capabilities: Capabilities,
            BaseUrl: options.BaseUrl));
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<string>> ListModelsAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<string> models = [options.ChatModel, embeddings.ModelName];

        return Task.FromResult(models);
    }

    public Task<string> GenerateChatCompletionAsync(
        ChatModelRequest request,
        CancellationToken cancellationToken = default)
    {
        return chat.GenerateAsync(request, cancellationToken);
    }

    public IAsyncEnumerable<string> GenerateChatCompletionStreamAsync(
        ChatModelRequest request,
        CancellationToken cancellationToken = default)
    {
        return chat.GenerateStreamAsync(request, cancellationToken);
    }

    public Task<float[]> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        return embeddings.GenerateAsync(text, cancellationToken);
    }
}
