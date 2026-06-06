using System.Text.Json;
using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Diagnostics;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Settings;
using KnowledgeApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Settings;

public sealed class SettingsService(
    IAppDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    ISettingsDefaultsProvider defaultsProvider,
    SettingsValidator validator,
    IOperationLogRepository operationLogRepository) : ISettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<AppSettingsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        Dictionary<string, string>? storedSettings = await dbContext.AppSettings
            .Where(x => SettingsKeys.KnownKeys.Contains(x.Key))
            .ToDictionaryAsync(x => x.Key, x => x.Value, cancellationToken);

        AppSettingsDto? defaults = defaultsProvider.GetDefaults();

        return new AppSettingsDto(
            Appearance: new AppearanceSettingsDto(
                Theme: GetString(storedSettings, SettingsKeys.Theme, defaults.Appearance.Theme)),
            Ai: new AiSettingsDto(
                Provider: GetString(storedSettings, SettingsKeys.AiProvider, defaults.Ai.Provider),
                ChatModel: GetString(storedSettings, SettingsKeys.AiChatModel, defaults.Ai.ChatModel),
                EmbeddingModel: GetString(storedSettings, SettingsKeys.AiEmbeddingModel, defaults.Ai.EmbeddingModel),
                RuntimePath: GetString(storedSettings, SettingsKeys.AiRuntimePath, defaults.Ai.RuntimePath),
                ModelsPath: GetString(storedSettings, SettingsKeys.AiModelsPath, defaults.Ai.ModelsPath)),
            RuntimePaths: new RuntimePathsSettingsDto(
                DataPath: GetString(storedSettings, SettingsKeys.RuntimeDataPath, defaults.RuntimePaths.DataPath),
                DatabasePath: GetString(storedSettings, SettingsKeys.RuntimeDatabasePath, defaults.RuntimePaths.DatabasePath),
                FilesPath: GetString(storedSettings, SettingsKeys.RuntimeFilesPath, defaults.RuntimePaths.FilesPath),
                IndexPath: GetString(storedSettings, SettingsKeys.RuntimeIndexPath, defaults.RuntimePaths.IndexPath),
                LogsPath: GetString(storedSettings, SettingsKeys.RuntimeLogsPath, defaults.RuntimePaths.LogsPath)),
            Sync: new SyncSettingsDto(
                Enabled: GetBool(storedSettings, SettingsKeys.SyncEnabled, defaults.Sync.Enabled),
                AutoSync: GetBool(storedSettings, SettingsKeys.SyncAutoSync, defaults.Sync.AutoSync)),
            WatchedFolders: new WatchedFoldersSettingsDto(
                Enabled: GetBool(
                    storedSettings,
                    SettingsKeys.WatchedFoldersEnabled,
                    defaults.WatchedFolders.Enabled),
                DebounceMilliseconds: GetInt(
                    storedSettings,
                    SettingsKeys.WatchedFoldersDebounceMilliseconds,
                    defaults.WatchedFolders.DebounceMilliseconds),
                DeletePolicy: GetString(
                    storedSettings,
                    SettingsKeys.WatchedFoldersDeletePolicy,
                    defaults.WatchedFolders.DeletePolicy),
                Folders: GetWatchedFolders(
                    storedSettings,
                    SettingsKeys.WatchedFoldersFoldersJson,
                    defaults.WatchedFolders.Folders)));
    }

    public async Task<Result> UpdateAsync(AppSettingsDto request, CancellationToken cancellationToken = default)
    {
        Result validation = validator.Validate(request);

        if (!validation.IsSuccess)
        {
            return validation;
        }

        Dictionary<string, AppSetting>? storedSettings = await dbContext.AppSettings
            .Where(x => SettingsKeys.KnownKeys.Contains(x.Key))
            .ToDictionaryAsync(x => x.Key, x => x, cancellationToken);

        Upsert(storedSettings, SettingsKeys.Theme, request.Appearance.Theme);

        Upsert(storedSettings, SettingsKeys.AiProvider, request.Ai.Provider);
        Upsert(storedSettings, SettingsKeys.AiChatModel, request.Ai.ChatModel);
        Upsert(storedSettings, SettingsKeys.AiEmbeddingModel, request.Ai.EmbeddingModel);
        Upsert(storedSettings, SettingsKeys.AiRuntimePath, request.Ai.RuntimePath);
        Upsert(storedSettings, SettingsKeys.AiModelsPath, request.Ai.ModelsPath);

        Upsert(storedSettings, SettingsKeys.RuntimeDataPath, request.RuntimePaths.DataPath);
        Upsert(storedSettings, SettingsKeys.RuntimeDatabasePath, request.RuntimePaths.DatabasePath);
        Upsert(storedSettings, SettingsKeys.RuntimeFilesPath, request.RuntimePaths.FilesPath);
        Upsert(storedSettings, SettingsKeys.RuntimeIndexPath, request.RuntimePaths.IndexPath);
        Upsert(storedSettings, SettingsKeys.RuntimeLogsPath, request.RuntimePaths.LogsPath);

        Upsert(storedSettings, SettingsKeys.SyncEnabled, request.Sync.Enabled.ToString());
        Upsert(storedSettings, SettingsKeys.SyncAutoSync, request.Sync.AutoSync.ToString());

        Upsert(storedSettings, SettingsKeys.WatchedFoldersEnabled, request.WatchedFolders.Enabled.ToString());
        Upsert(
            storedSettings,
            SettingsKeys.WatchedFoldersDebounceMilliseconds,
            request.WatchedFolders.DebounceMilliseconds.ToString());
        Upsert(storedSettings, SettingsKeys.WatchedFoldersDeletePolicy, request.WatchedFolders.DeletePolicy);
        Upsert(
            storedSettings,
            SettingsKeys.WatchedFoldersFoldersJson,
            JsonSerializer.Serialize(request.WatchedFolders.Folders, JsonOptions));

        await operationLogRepository.AddAsync(new OperationLog
        {
            OperationType = "Settings.Update",
            EntityType = "Settings",
            EntityId = "Global",
            Message = "Updated application settings",
            MetadataJson = "{}"
        }, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private static string GetString(IReadOnlyDictionary<string, string> settings, string key, string fallback)
    {
        return settings.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : fallback;
    }

    private static bool GetBool(IReadOnlyDictionary<string, string> settings, string key, bool fallback)
    {
        return settings.TryGetValue(key, out string? value) && bool.TryParse(value, out bool parsed)
            ? parsed
            : fallback;
    }

    private static int GetInt(IReadOnlyDictionary<string, string> settings, string key, int fallback)
    {
        return settings.TryGetValue(key, out string? value) && int.TryParse(value, out int parsed)
            ? parsed
            : fallback;
    }

    private static IReadOnlyList<WatchedFolderDto> GetWatchedFolders(
        IReadOnlyDictionary<string, string> settings,
        string key,
        IReadOnlyList<WatchedFolderDto> fallback)
    {
        if (!settings.TryGetValue(key, out string? value) || string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        try
        {
            IReadOnlyList<WatchedFolderDto>? folders =
                JsonSerializer.Deserialize<IReadOnlyList<WatchedFolderDto>>(value, JsonOptions);

            return folders ?? fallback;
        }
        catch (JsonException)
        {
            return fallback;
        }
    }

    private void Upsert(IReadOnlyDictionary<string, AppSetting> storedSettings, string key, string value)
    {
        if (storedSettings.TryGetValue(key, out AppSetting? setting))
        {
            setting.Value = value;
            setting.UpdatedAt = dateTimeProvider.UtcNow;
            return;
        }

        dbContext.AppSettings.Add(new AppSetting
        {
            Key = key,
            Value = value,
            CreatedAt = dateTimeProvider.UtcNow,
        });
    }
}
