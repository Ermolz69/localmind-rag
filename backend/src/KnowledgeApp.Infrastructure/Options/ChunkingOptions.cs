namespace KnowledgeApp.Infrastructure.Options;

public sealed class ChunkingOptions
{
    public const string SectionName = "Chunking";

    public string Strategy { get; set; } = "StructureAware";

    public int TargetChunkCharacters { get; set; } = 1200;

    public int MaxChunkCharacters { get; set; } = 1600;

    public int MinChunkCharacters { get; set; } = 200;

    public int OverlapCharacters { get; set; } = 150;

    public bool ApplyOverlapOnlyOnForcedSplit { get; set; } = true;

    public bool PreserveHeadings { get; set; } = true;
}
