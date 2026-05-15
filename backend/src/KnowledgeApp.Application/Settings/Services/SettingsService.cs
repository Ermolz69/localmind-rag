using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Settings;
using KnowledgeApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Settings;

public sealed class SettingsService(
    IAppDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    ISettingsDefaultsProvider defaultsProvider,
    SettingsValidator validator) : ISettingsService
{
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
                AutoSync: GetBool(storedSettings, SettingsKeys.SyncAutoSync, defaults.Sync.AutoSync)));
    }

    public async Task UpdateAsync(AppSettingsDto request, CancellationToken cancellationToken = default)
    {
        validator.Validate(request);

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

        await dbContext.SaveChangesAsync(cancellationToken);
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
