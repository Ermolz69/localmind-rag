namespace KnowledgeApp.Contracts.Settings;

public sealed record AiSettingsDto(
    string Provider,
    string ChatModel,
    string EmbeddingModel,
    string RuntimePath,
    string ModelsPath);


