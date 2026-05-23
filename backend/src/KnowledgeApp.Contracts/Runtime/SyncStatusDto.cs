namespace KnowledgeApp.Contracts.Runtime;

/// <summary>Current remote sync status.</summary>
/// <param name="Enabled">True when remote sync is enabled in settings.</param>
/// <param name="Online">True when network connectivity is available.</param>
/// <param name="PendingOperations">Number of local operations waiting to sync.</param>
/// <param name="Status">Human-readable sync state.</param>
public sealed record SyncStatusDto(bool Enabled, bool Online, int PendingOperations, string Status);

