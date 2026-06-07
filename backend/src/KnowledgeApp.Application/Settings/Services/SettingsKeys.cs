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

    public const string WatchedFoldersEnabled = "WatchedFolders.Enabled";
    public const string WatchedFoldersDebounceMilliseconds = "WatchedFolders.DebounceMilliseconds";
    public const string WatchedFoldersDeletePolicy = "WatchedFolders.DeletePolicy";
    public const string WatchedFoldersFoldersJson = "WatchedFolders.FoldersJson";

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
        WatchedFoldersEnabled,
        WatchedFoldersDebounceMilliseconds,
        WatchedFoldersDeletePolicy,
        WatchedFoldersFoldersJson,
    ];
}
