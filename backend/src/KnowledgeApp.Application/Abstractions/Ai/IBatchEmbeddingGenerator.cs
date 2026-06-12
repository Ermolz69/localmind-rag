namespace KnowledgeApp.Application.Abstractions;

public interface IBatchEmbeddingGenerator : IEmbeddingGenerator
{
    Task<IReadOnlyList<float[]>> GenerateBatchAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default);
}
