namespace KnowledgeApp.Infrastructure.Options;

public sealed class ChunkingProfile
{
    public int TargetTokens { get; set; } = 420;

    public int MaxTokens { get; set; } = 700;

    public int MinTokens { get; set; } = 120;

    public int OverlapTokens { get; set; } = 80;
}
