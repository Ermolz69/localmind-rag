namespace KnowledgeApp.Infrastructure.Options;

public sealed class LocalRuntimeOptions
{
    public bool Portable { get; set; } = true;
    public string DataPath { get; set; } = "runtime/app/data";
    public string DatabasePath { get; set; } = "runtime/app/data/knowledge-app.db";
    public string FilesPath { get; set; } = "runtime/app/files";
    public string IndexPath { get; set; } = "runtime/app/indexes";
    public string LogsPath { get; set; } = "runtime/app/logs";
}

public sealed class AiOptions
{
    public string Provider { get; set; } = "LlamaCpp";
    public string BaseUrl { get; set; } = "http://127.0.0.1:11435";
    public string ChatModel { get; set; } = "qwen2.5-3b-instruct";
    public string EmbeddingModel { get; set; } = "nomic-embed-text";
    public double Temperature { get; set; } = 0.2;
    public int TopK { get; set; } = 40;
    public int ContextSize { get; set; } = 8192;
    public bool AutoStartRuntime { get; set; } = true;
    public string RuntimePath { get; set; } = "runtime/ai/bin/llama-server.exe";
    public string ModelsPath { get; set; } = "runtime/ai/models";
}
