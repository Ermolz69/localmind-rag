namespace KnowledgeApp.Contracts.Settings;

/// <summary>Remote synchronization settings.</summary>
/// <param name="Enabled">True when sync is enabled.</param>
/// <param name="AutoSync">True when LocalMind should sync automatically.</param>
public sealed record SyncSettingsDto(bool Enabled, bool AutoSync);

