using KnowledgeApp.Application.Abstractions;

namespace KnowledgeApp.Infrastructure.Services;

public sealed class ProviderEmbeddingGenerator(IAiRuntimeProviderRegistry registry) : IBatchEmbeddingGenerator
{
    public string ModelName => registry.GetSelectedProvider().EmbeddingModelName;

    public Task<float[]> GenerateAsync(string text, CancellationToken cancellationToken = default)
    {
        return registry.GetSelectedProvider().GenerateEmbeddingAsync(text, cancellationToken);
    }

    public async Task<IReadOnlyList<float[]>> GenerateBatchAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
    {
        IAiRuntimeProvider provider = registry.GetSelectedProvider();

        if (provider is AiRuntimeManager manager)
        {
            return await manager.GenerateEmbeddingBatchAsync(texts, cancellationToken);
        }

        List<float[]> embeddings = new(texts.Count);
        foreach (string text in texts)
        {
            embeddings.Add(await provider.GenerateEmbeddingAsync(text, cancellationToken));
        }

        return embeddings;
    }
}
