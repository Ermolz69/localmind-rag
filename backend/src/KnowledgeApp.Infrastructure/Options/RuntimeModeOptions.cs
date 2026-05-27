namespace KnowledgeApp.Infrastructure.Options;

public sealed class RuntimeModeOptions
{
    public const string SectionName = "LocalRuntime";

    public bool Portable { get; set; } = true;
}
