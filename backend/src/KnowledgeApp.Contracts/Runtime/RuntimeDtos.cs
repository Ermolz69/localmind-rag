namespace KnowledgeApp.Contracts.Runtime;

public sealed record RuntimeStatusDto(bool LocalApiReady, string AiRuntimeStatus, bool ModelsAvailable, bool OfflineMode);
public sealed record SyncStatusDto(bool Enabled, bool Online, int PendingOperations, string Status);
