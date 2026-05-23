namespace KnowledgeApp.Contracts.Settings;

/// <summary>AI provider, model, and runtime path settings.</summary>
/// <param name="Provider">Configured AI provider name.</param>
/// <param name="ChatModel">Configured chat model name.</param>
/// <param name="EmbeddingModel">Configured embedding model name.</param>
/// <param name="RuntimePath">Path to the local AI runtime executable.</param>
/// <param name="ModelsPath">Path to local model files.</param>
public sealed record AiSettingsDto(
    string Provider,
    string ChatModel,
    string EmbeddingModel,
    string RuntimePath,
    string ModelsPath);

