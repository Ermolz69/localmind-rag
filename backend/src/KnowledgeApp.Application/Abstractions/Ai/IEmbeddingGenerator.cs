namespace KnowledgeApp.Application.Abstractions;

public interface IEmbeddingGenerator
{
    string ModelName { get; }

    Task<float[]> GenerateAsync(string text, CancellationToken cancellationToken = default);
}
