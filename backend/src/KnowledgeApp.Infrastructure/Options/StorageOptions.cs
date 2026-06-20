namespace KnowledgeApp.Infrastructure.Options;

public sealed class StorageOptions
{
    public const string SectionName = "LocalRuntime";

    public string DataPath { get; set; } = "runtime/app/data";

    public string FilesPath { get; set; } = "runtime/app/files";

    public string PreviewsPath { get; set; } = "runtime/app/previews";

    public string LogsPath { get; set; } = "runtime/app/logs";
}
