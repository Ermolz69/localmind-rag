namespace KnowledgeApp.Infrastructure.Options;

public sealed class VectorIndexOptions
{
    public const string SectionName = "LocalRuntime";

    public string IndexPath { get; set; } = "runtime/app/indexes";
}
