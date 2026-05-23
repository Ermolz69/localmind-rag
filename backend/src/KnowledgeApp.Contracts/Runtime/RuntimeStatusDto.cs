namespace KnowledgeApp.Contracts.Runtime;

public sealed record RuntimeStatusDto(
    bool LocalApiReady,
    string AiRuntimeStatus,
    bool ModelsAvailable,
    bool OfflineMode,
    string? RuntimePath = null,
    string? ModelPath = null,
    bool SetupRequired = false,
    string? SetupReason = null);

