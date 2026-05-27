namespace KnowledgeApp.Infrastructure.Options;

public sealed class DatabaseOptions
{
    public const string SectionName = "LocalRuntime";

    public string DatabasePath { get; set; } = "runtime/app/data/knowledge-app.db";
}
