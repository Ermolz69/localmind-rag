namespace KnowledgeApp.Contracts.Runtime;

public sealed record RuntimeStatusDto(bool LocalApiReady, string AiRuntimeStatus, bool ModelsAvailable, bool OfflineMode);


