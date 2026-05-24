using KnowledgeApp.Application.Abstractions;

namespace KnowledgeApp.Infrastructure.Services;

public sealed class ProviderEmbeddingGenerator(IAiRuntimeProviderRegistry registry) : IEmbeddingGenerator
{
    public string ModelName => registry.GetSelectedProvider().EmbeddingModelName;

    public Task<float[]> GenerateAsync(string text, CancellationToken cancellationToken = default)
    {
        return registry.GetSelectedProvider().GenerateEmbeddingAsync(text, cancellationToken);
    }
}
