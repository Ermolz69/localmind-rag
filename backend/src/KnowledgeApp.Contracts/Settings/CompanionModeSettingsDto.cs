namespace KnowledgeApp.Contracts.Settings;

/// <summary>
/// Companion Mode settings. Companion Mode is an opt-in mode that lets a phone
/// connect to LocalMind over the local network as a remote interface. It is
/// disabled by default and the desktop app stays local-only until the user
/// explicitly enables it.
/// </summary>
/// <param name="Enabled">True when phone connection (Companion Mode) is enabled.</param>
/// <param name="AllowedFolders">
/// Absolute folder paths the phone is allowed to browse and pick files from.
/// The phone can only see inside these folders, never the whole disk.
/// </param>
public sealed record CompanionModeSettingsDto(
    bool Enabled,
    IReadOnlyList<string>? AllowedFolders = null);
