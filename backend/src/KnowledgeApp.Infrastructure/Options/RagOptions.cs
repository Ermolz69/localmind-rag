namespace KnowledgeApp.Infrastructure.Options;

public sealed class RagOptions
{
    public const string SectionName = "Rag";

    public double MinimumSourceScore { get; set; } = 0.3;

    public double MaxSourceScoreDistance { get; set; } = 0.1;

    public bool EnableSemanticCache { get; set; }
}
