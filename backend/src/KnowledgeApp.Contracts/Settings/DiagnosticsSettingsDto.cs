namespace KnowledgeApp.Contracts.Settings;

/// <summary>Diagnostics settings.</summary>
/// <param name="Enabled">Whether diagnostics panel and page are enabled.</param>
public sealed record DiagnosticsSettingsDto(bool Enabled);
