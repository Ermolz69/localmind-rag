using System.Security.Cryptography;
using System.Text;

using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Infrastructure.Options;

using Microsoft.Extensions.Options;

namespace KnowledgeApp.Infrastructure.Services;

public sealed class StubEmbeddingGenerator : IEmbeddingGenerator
{
    private const string DefaultModelName = "BGE-M3";

    private readonly string modelName;

    public StubEmbeddingGenerator() : this(DefaultModelName)
    {
    }

    public StubEmbeddingGenerator(IOptions<EmbeddingOptions> options)
        : this(options.Value.EmbeddingModel)
    {
    }

    private StubEmbeddingGenerator(string modelName)
    {
        this.modelName = string.IsNullOrWhiteSpace(modelName)
            ? DefaultModelName
            : modelName;
    }

    public string ModelName => modelName;

    public Task<float[]> GenerateAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));

        return Task.FromResult(
            bytes.Select(value => (float)value / byte.MaxValue).ToArray());
    }
}
