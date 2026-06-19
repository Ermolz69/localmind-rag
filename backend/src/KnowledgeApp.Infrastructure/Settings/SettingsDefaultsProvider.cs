using KnowledgeApp.Application.Settings;
using KnowledgeApp.Contracts.Settings;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.Infrastructure.Settings;

public sealed class SettingsDefaultsProvider(
    IOptions<StorageOptions> storageOptions,
    IOptions<DatabaseOptions> databaseOptions,
    IOptions<VectorIndexOptions> vectorIndexOptions,
    IOptions<RuntimeOptions> runtimeOptions,
    IOptions<EmbeddingOptions> embeddingOptions,
    IConfiguration configuration) : ISettingsDefaultsProvider
{
    public AppSettingsDto GetDefaults()
    {
        StorageOptions storage = storageOptions.Value;
        DatabaseOptions database = databaseOptions.Value;
        VectorIndexOptions vectorIndex = vectorIndexOptions.Value;
        RuntimeOptions runtime = runtimeOptions.Value;
        EmbeddingOptions embedding = embeddingOptions.Value;

        IConfigurationSection observability = configuration.GetSection("Observability");

        return new AppSettingsDto(
            Appearance: new AppearanceSettingsDto(AppTheme.System.ToString()),
            Ai: new AiSettingsDto(
                runtime.Provider,
                runtime.ChatModel,
                embedding.EmbeddingModel,
                runtime.RuntimePath,
                embedding.ModelsPath),
            RuntimePaths: new RuntimePathsSettingsDto(
                storage.DataPath,
                database.DatabasePath,
                storage.FilesPath,
                vectorIndex.IndexPath,
                storage.LogsPath),
            Sync: new SyncSettingsDto(false, false),
            Diagnostics: new DiagnosticsSettingsDto(
                Enabled: GetBool(observability, "Enabled", true),
                DeveloperModeEnabled: false,
                MinimumLogLevel: GetString(observability, "MinimumLevel", "Information"),
                UseSeparateLogFiles: GetBool(observability, "UseSeparateLogFiles", false),
                EnableErrorLogs: GetBool(observability, "EnableErrorLogs", true),
                EnableSqlLogs: GetBool(observability, "EnableSqlLogs", false),
                EnableHttpLogs: GetBool(observability, "EnableHttpLogs", true),
                EnableDiagnosticEventLogs: GetBool(observability, "EnableDiagnosticEventLogs", false),
                EnableDebugTrace: GetBool(observability, "EnableDebugTrace", false)),
            WatchedFolders: new WatchedFoldersSettingsDto(
                Enabled: false,
                DebounceMilliseconds: 1000,
                DeletePolicy: "MarkDeleted",
                Folders: Array.Empty<WatchedFolderDto>(),
                IgnoredFolders: [".git", "node_modules", "bin", "obj"],
                IgnoredPatterns: ["~$*", "*.tmp", "*.bak"],
                MaxFileSizeMb: 100,
                AllowedExtensions: null,
                StorageMode: WatchedFolderStorageModes.LinkOnly));
    }

    private static bool GetBool(IConfiguration section, string key, bool fallback)
    {
        return bool.TryParse(section[key], out bool value) ? value : fallback;
    }

    private static string GetString(IConfiguration section, string key, string fallback)
    {
        string? value = section[key];
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }
}
