namespace KnowledgeApp.Infrastructure.Options;

public sealed class EmbeddingOptions
{
    public const string SectionName = "Ai";

    public string EmbeddingProvider { get; set; } = "LlamaCpp";

    public string EmbeddingModel { get; set; } = "bge-m3";

    public string EmbeddingModelManifest { get; set; } = "bge-m3-q4-k-m";

    public string ModelsPath { get; set; } = "runtime/ai/models";

    public int TopK { get; set; } = 40;

    public int EmbeddingBatchSize { get; set; } = 16;
}
