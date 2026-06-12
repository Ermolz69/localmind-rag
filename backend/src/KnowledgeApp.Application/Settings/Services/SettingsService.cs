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
            Diagnostics: new DiagnosticsSettingsDto(
                Enabled: GetBool(storedSettings, SettingsKeys.DiagnosticsEnabled, defaults.Diagnostics.Enabled)),
            WatchedFolders: new WatchedFoldersSettingsDto(
                Enabled: GetBool(
                    storedSettings,
                    SettingsKeys.WatchedFoldersEnabled,
                    defaults.WatchedFolders!.Enabled),
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
                    defaults.WatchedFolders.Folders),
                IgnoredFolders: GetStringList(
                    storedSettings,
                    SettingsKeys.WatchedFoldersIgnoredFoldersJson,
                    defaults.WatchedFolders.IgnoredFolders),
                IgnoredPatterns: GetStringList(
                    storedSettings,
                    SettingsKeys.WatchedFoldersIgnoredPatternsJson,
                    defaults.WatchedFolders.IgnoredPatterns),
                MaxFileSizeMb: GetNullableInt(
                    storedSettings,
                    SettingsKeys.WatchedFoldersMaxFileSizeMb,
                    defaults.WatchedFolders.MaxFileSizeMb),
                AllowedExtensions: GetStringList(
                    storedSettings,
                    SettingsKeys.WatchedFoldersAllowedExtensionsJson,
                    defaults.WatchedFolders.AllowedExtensions),
                StorageMode: GetString(
                    storedSettings,
                    SettingsKeys.WatchedFoldersStorageMode,
                    defaults.WatchedFolders.StorageMode)));
    }

    public async Task<Result> UpdateAsync(AppSettingsDto request, CancellationToken cancellationToken = default)
    {
        AppSettingsDto normalizedRequest = NormalizeRequest(request);

        Result validation = validator.Validate(normalizedRequest);

        if (!validation.IsSuccess)
        {
            return validation;
        }

        Dictionary<string, AppSetting>? storedSettings = await dbContext.AppSettings
            .Where(x => SettingsKeys.KnownKeys.Contains(x.Key))
            .ToDictionaryAsync(x => x.Key, x => x, cancellationToken);

        Upsert(storedSettings, SettingsKeys.Theme, normalizedRequest.Appearance.Theme);

        Upsert(storedSettings, SettingsKeys.AiProvider, normalizedRequest.Ai.Provider);
        Upsert(storedSettings, SettingsKeys.AiChatModel, normalizedRequest.Ai.ChatModel);
        Upsert(storedSettings, SettingsKeys.AiEmbeddingModel, normalizedRequest.Ai.EmbeddingModel);
        Upsert(storedSettings, SettingsKeys.AiRuntimePath, normalizedRequest.Ai.RuntimePath);
        Upsert(storedSettings, SettingsKeys.AiModelsPath, normalizedRequest.Ai.ModelsPath);

        Upsert(storedSettings, SettingsKeys.RuntimeDataPath, normalizedRequest.RuntimePaths.DataPath);
        Upsert(storedSettings, SettingsKeys.RuntimeDatabasePath, normalizedRequest.RuntimePaths.DatabasePath);
        Upsert(storedSettings, SettingsKeys.RuntimeFilesPath, normalizedRequest.RuntimePaths.FilesPath);
        Upsert(storedSettings, SettingsKeys.RuntimeIndexPath, normalizedRequest.RuntimePaths.IndexPath);
        Upsert(storedSettings, SettingsKeys.RuntimeLogsPath, normalizedRequest.RuntimePaths.LogsPath);

        Upsert(storedSettings, SettingsKeys.SyncEnabled, normalizedRequest.Sync.Enabled.ToString());
        Upsert(storedSettings, SettingsKeys.SyncAutoSync, normalizedRequest.Sync.AutoSync.ToString());

        Upsert(storedSettings, SettingsKeys.DiagnosticsEnabled, normalizedRequest.Diagnostics.Enabled.ToString());

        Upsert(storedSettings, SettingsKeys.WatchedFoldersEnabled, normalizedRequest.WatchedFolders!.Enabled.ToString());
        Upsert(
            storedSettings,
            SettingsKeys.WatchedFoldersDebounceMilliseconds,
            normalizedRequest.WatchedFolders.DebounceMilliseconds.ToString());
        Upsert(storedSettings, SettingsKeys.WatchedFoldersDeletePolicy, normalizedRequest.WatchedFolders.DeletePolicy);
        Upsert(
            storedSettings,
            SettingsKeys.WatchedFoldersFoldersJson,
            JsonSerializer.Serialize(normalizedRequest.WatchedFolders.Folders, JsonOptions));
        Upsert(
            storedSettings,
            SettingsKeys.WatchedFoldersIgnoredFoldersJson,
            JsonSerializer.Serialize(normalizedRequest.WatchedFolders.IgnoredFolders, JsonOptions));
        Upsert(
            storedSettings,
            SettingsKeys.WatchedFoldersIgnoredPatternsJson,
            JsonSerializer.Serialize(normalizedRequest.WatchedFolders.IgnoredPatterns, JsonOptions));
        Upsert(
            storedSettings,
            SettingsKeys.WatchedFoldersMaxFileSizeMb,
            normalizedRequest.WatchedFolders.MaxFileSizeMb?.ToString() ?? string.Empty);
        Upsert(
            storedSettings,
            SettingsKeys.WatchedFoldersAllowedExtensionsJson,
            JsonSerializer.Serialize(normalizedRequest.WatchedFolders.AllowedExtensions, JsonOptions));
        Upsert(
            storedSettings,
            SettingsKeys.WatchedFoldersStorageMode,
            normalizedRequest.WatchedFolders.StorageMode);

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

    private AppSettingsDto NormalizeRequest(AppSettingsDto request)
    {
        AppSettingsDto defaults = defaultsProvider.GetDefaults();

        return request with
        {
            WatchedFolders = request.WatchedFolders ?? defaults.WatchedFolders
        };
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

    private static IReadOnlyList<string>? GetStringList(
        IReadOnlyDictionary<string, string> settings,
        string key,
        IReadOnlyList<string>? fallback)
    {
        if (!settings.TryGetValue(key, out string? value) || string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        try
        {
            IReadOnlyList<string>? list = JsonSerializer.Deserialize<IReadOnlyList<string>>(value, JsonOptions);
            return list ?? fallback;
        }
        catch (JsonException)
        {
            return fallback;
        }
    }

    private static int? GetNullableInt(
        IReadOnlyDictionary<string, string> settings,
        string key,
        int? fallback)
    {
        return settings.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value) && int.TryParse(value, out int parsed)
            ? parsed
            : fallback;
    }

    private void Upsert(IReadOnlyDictionary<string, AppSetting> storedSettings, string key, string value)
    {
        if (storedSettings.TryGetValue(key, out AppSetting? setting))
        {
            setting.Value = value;
            setting.UpdatedAt = dateTimeProvider.UtcNow.UtcDateTime;
            return;
        }

        dbContext.AppSettings.Add(new AppSetting
        {
            Key = key,
            Value = value,
            CreatedAt = dateTimeProvider.UtcNow.UtcDateTime,
        });
    }
}
