namespace KnowledgeApp.Application.Settings;

public static class SettingsKeys
{
    public const string Theme = "App.Theme";

    public const string AiProvider = "Ai.Provider";
    public const string AiChatModel = "Ai.ChatModel";
    public const string AiEmbeddingModel = "Ai.EmbeddingModel";
    public const string AiRuntimePath = "Ai.RuntimePath";
    public const string AiModelsPath = "Ai.ModelsPath";

    public const string RuntimeDataPath = "Runtime.DataPath";
    public const string RuntimeDatabasePath = "Runtime.DatabasePath";
    public const string RuntimeFilesPath = "Runtime.FilesPath";
    public const string RuntimeIndexPath = "Runtime.IndexPath";
    public const string RuntimeLogsPath = "Runtime.LogsPath";

    public const string SyncEnabled = "Sync.Enabled";
    public const string SyncAutoSync = "Sync.AutoSync";

    public const string DiagnosticsEnabled = "Diagnostics.Enabled";
    public const string DiagnosticsDeveloperModeEnabled = "Diagnostics.DeveloperModeEnabled";
    public const string DiagnosticsMinimumLogLevel = "Diagnostics.MinimumLogLevel";
    public const string DiagnosticsUseSeparateLogFiles = "Diagnostics.UseSeparateLogFiles";
    public const string DiagnosticsEnableErrorLogs = "Diagnostics.EnableErrorLogs";
    public const string DiagnosticsEnableSqlLogs = "Diagnostics.EnableSqlLogs";
    public const string DiagnosticsEnableHttpLogs = "Diagnostics.EnableHttpLogs";
    public const string DiagnosticsEnableDiagnosticEventLogs = "Diagnostics.EnableDiagnosticEventLogs";
    public const string DiagnosticsEnableDebugTrace = "Diagnostics.EnableDebugTrace";
    public const string DiagnosticsLogRetainedDays = "Diagnostics.LogRetainedDays";

    public const string WatchedFoldersEnabled = "WatchedFolders.Enabled";
    public const string WatchedFoldersDebounceMilliseconds = "WatchedFolders.DebounceMilliseconds";
    public const string WatchedFoldersDeletePolicy = "WatchedFolders.DeletePolicy";
    public const string WatchedFoldersFoldersJson = "WatchedFolders.FoldersJson";
    public const string WatchedFoldersIgnoredFoldersJson = "WatchedFolders.IgnoredFoldersJson";
    public const string WatchedFoldersIgnoredPatternsJson = "WatchedFolders.IgnoredPatternsJson";
    public const string WatchedFoldersMaxFileSizeMb = "WatchedFolders.MaxFileSizeMb";
    public const string WatchedFoldersAllowedExtensionsJson = "WatchedFolders.AllowedExtensionsJson";
    public const string WatchedFoldersStorageMode = "WatchedFolders.StorageMode";

    public const string CompanionModeEnabled = "CompanionMode.Enabled";

    public static readonly string[] KnownKeys =
    [
        Theme,
        AiProvider,
        AiChatModel,
        AiEmbeddingModel,
        AiRuntimePath,
        AiModelsPath,
        RuntimeDataPath,
        RuntimeDatabasePath,
        RuntimeFilesPath,
        RuntimeIndexPath,
        RuntimeLogsPath,
        SyncEnabled,
        SyncAutoSync,
        DiagnosticsEnabled,
        DiagnosticsDeveloperModeEnabled,
        DiagnosticsMinimumLogLevel,
        DiagnosticsUseSeparateLogFiles,
        DiagnosticsEnableErrorLogs,
        DiagnosticsEnableSqlLogs,
        DiagnosticsEnableHttpLogs,
        DiagnosticsEnableDiagnosticEventLogs,
        DiagnosticsEnableDebugTrace,
        DiagnosticsLogRetainedDays,
        WatchedFoldersEnabled,
        WatchedFoldersDebounceMilliseconds,
        WatchedFoldersDeletePolicy,
        WatchedFoldersFoldersJson,
        WatchedFoldersIgnoredFoldersJson,
        WatchedFoldersIgnoredPatternsJson,
        WatchedFoldersMaxFileSizeMb,
        WatchedFoldersAllowedExtensionsJson,
        WatchedFoldersStorageMode,
        CompanionModeEnabled,
    ];
}
