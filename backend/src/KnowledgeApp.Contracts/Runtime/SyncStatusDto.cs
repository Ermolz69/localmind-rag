namespace KnowledgeApp.Contracts.Runtime;

public sealed record SyncStatusDto(bool Enabled, bool Online, int PendingOperations, string Status);


