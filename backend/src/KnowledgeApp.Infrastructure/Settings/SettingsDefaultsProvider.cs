using KnowledgeApp.Application.Settings;
using KnowledgeApp.Contracts.Settings;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.Infrastructure.Settings;

public sealed class SettingsDefaultsProvider(
    IOptions<StorageOptions> storageOptions,
    IOptions<DatabaseOptions> databaseOptions,
    IOptions<VectorIndexOptions> vectorIndexOptions,
    IOptions<RuntimeOptions> runtimeOptions,
    IOptions<EmbeddingOptions> embeddingOptions) : ISettingsDefaultsProvider
{
    public AppSettingsDto GetDefaults()
    {
        StorageOptions storage = storageOptions.Value;
        DatabaseOptions database = databaseOptions.Value;
        VectorIndexOptions vectorIndex = vectorIndexOptions.Value;
        RuntimeOptions runtime = runtimeOptions.Value;
        EmbeddingOptions embedding = embeddingOptions.Value;

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
            WatchedFolders: new WatchedFoldersSettingsDto(
                Enabled: false,
                DebounceMilliseconds: 1000,
                DeletePolicy: "MarkDeleted",
                Folders: []));
    }
}
