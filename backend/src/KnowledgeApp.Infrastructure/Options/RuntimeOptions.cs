namespace KnowledgeApp.Infrastructure.Options;

public sealed class RuntimeOptions
{
    public const string SectionName = "Ai";

    public const string DefaultRuntimeDownloadUrl =
        "https://github.com/ggml-org/llama.cpp/releases/download/b9222/llama-b9222-bin-win-vulkan-x64.zip";

    public string Provider { get; set; } = "LlamaCpp";

    public string BaseUrl { get; set; } = "http://127.0.0.1:11435";

    public string ChatBaseUrl { get; set; } = "http://127.0.0.1:11436";

    public string EmbeddingBaseUrl { get; set; } = "http://127.0.0.1:11435";

    public string ChatModel { get; set; } = "meta-llama/Llama-3.2-3B-Instruct";

    public string ChatModelManifest { get; set; } = "llama-3.2-3b-instruct-q4-k-m";

    public double Temperature { get; set; } = 0.2;

    public int ContextSize { get; set; } = 8192;

    public bool AutoStartRuntime { get; set; } = true;

    public string RuntimePath { get; set; } = "runtime/ai/bin/llama-server.exe";

    public string RuntimeDownloadUrl { get; set; } = DefaultRuntimeDownloadUrl;
}
