namespace KnowledgeApp.Infrastructure.Options;

public sealed class ChunkingOptions
{
    public const string SectionName = "Chunking";

    public ChunkingTokenizerOptions Tokenizer { get; set; } = new();

    public int ChunkingVersion { get; set; } = 1;

    public string ChunkingAlgorithmId { get; set; } = "structure-aware-token-v1";

    public ChunkingProfile Default { get; set; } = new()
    {
        TargetTokens = 420,
        MaxTokens = 700,
        MinTokens = 120,
        OverlapTokens = 80
    };

    public ChunkingProfile Code { get; set; } = new()
    {
        TargetTokens = 280,
        MaxTokens = 520,
        MinTokens = 80,
        OverlapTokens = 40
    };

    public ChunkingProfile Table { get; set; } = new()
    {
        TargetTokens = 500,
        MaxTokens = 900,
        MinTokens = 100,
        OverlapTokens = 30
    };

    public ChunkingProfile Slide { get; set; } = new()
    {
        TargetTokens = 300,
        MaxTokens = 500,
        MinTokens = 50,
        OverlapTokens = 0
    };
}
