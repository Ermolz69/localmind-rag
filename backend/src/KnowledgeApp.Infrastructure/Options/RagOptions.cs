namespace KnowledgeApp.Infrastructure.Options;

public sealed class RagOptions
{
    public const string SectionName = "Rag";

    public double MinimumSourceScore { get; set; } = 0.65;
}
