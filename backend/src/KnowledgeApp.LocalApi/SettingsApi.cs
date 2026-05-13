using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Settings;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Options;
using KnowledgeApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.LocalApi;

public static class SettingsApi
{
    private static readonly string[] KnownKeys =
    [
        SettingsKeys.Theme,
        SettingsKeys.AiProvider,
        SettingsKeys.AiChatModel,
        SettingsKeys.AiEmbeddingModel,
        SettingsKeys.AiRuntimePath,
        SettingsKeys.AiModelsPath,
        SettingsKeys.RuntimeDataPath,
        SettingsKeys.RuntimeDatabasePath,
        SettingsKeys.RuntimeFilesPath,
        SettingsKeys.RuntimeIndexPath,
        SettingsKeys.RuntimeLogsPath,
        SettingsKeys.SyncEnabled,
        SettingsKeys.SyncAutoSync,
    ];

    public static async Task<Ok<AppSettingsDto>> GetAsync(
        AppDbContext db,
        IOptions<LocalRuntimeOptions> runtimeOptions,
        IOptions<AiOptions> aiOptions,
        CancellationToken cancellationToken)
    {
        var storedSettings = await db.AppSettings
            .Where(x => KnownKeys.Contains(x.Key))
            .ToDictionaryAsync(x => x.Key, x => x.Value, cancellationToken);

        var runtime = runtimeOptions.Value;
        var ai = aiOptions.Value;

        var response = new AppSettingsDto(
            Appearance: new AppearanceSettingsDto(
                Theme: GetString(storedSettings, SettingsKeys.Theme, AppTheme.System.ToString())),
            Ai: new AiSettingsDto(
                Provider: GetString(storedSettings, SettingsKeys.AiProvider, ai.Provider),
                ChatModel: GetString(storedSettings, SettingsKeys.AiChatModel, ai.ChatModel),
                EmbeddingModel: GetString(storedSettings, SettingsKeys.AiEmbeddingModel, ai.EmbeddingModel),
                RuntimePath: GetString(storedSettings, SettingsKeys.AiRuntimePath, ai.RuntimePath),
                ModelsPath: GetString(storedSettings, SettingsKeys.AiModelsPath, ai.ModelsPath)),
            RuntimePaths: new RuntimePathsSettingsDto(
                DataPath: GetString(storedSettings, SettingsKeys.RuntimeDataPath, runtime.DataPath),
                DatabasePath: GetString(storedSettings, SettingsKeys.RuntimeDatabasePath, runtime.DatabasePath),
                FilesPath: GetString(storedSettings, SettingsKeys.RuntimeFilesPath, runtime.FilesPath),
                IndexPath: GetString(storedSettings, SettingsKeys.RuntimeIndexPath, runtime.IndexPath),
                LogsPath: GetString(storedSettings, SettingsKeys.RuntimeLogsPath, runtime.LogsPath)),
            Sync: new SyncSettingsDto(
                Enabled: GetBool(storedSettings, SettingsKeys.SyncEnabled, false),
                AutoSync: GetBool(storedSettings, SettingsKeys.SyncAutoSync, false)));

        return TypedResults.Ok(response);
    }

    public static async Task<Results<NoContent, ValidationProblem>> PutAsync(
        AppSettingsDto request,
        AppDbContext db,
        IDateTimeProvider dateTimeProvider,
        CancellationToken cancellationToken)
    {
        var errors = Validate(request);

        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var storedSettings = await db.AppSettings
            .Where(x => KnownKeys.Contains(x.Key))
            .ToDictionaryAsync(x => x.Key, x => x, cancellationToken);

        Upsert(storedSettings, db, SettingsKeys.Theme, request.Appearance.Theme, dateTimeProvider.UtcNow);
        Upsert(storedSettings, db, SettingsKeys.AiProvider, request.Ai.Provider, dateTimeProvider.UtcNow);
        Upsert(storedSettings, db, SettingsKeys.AiChatModel, request.Ai.ChatModel, dateTimeProvider.UtcNow);
        Upsert(storedSettings, db, SettingsKeys.AiEmbeddingModel, request.Ai.EmbeddingModel, dateTimeProvider.UtcNow);
        Upsert(storedSettings, db, SettingsKeys.AiRuntimePath, request.Ai.RuntimePath, dateTimeProvider.UtcNow);
        Upsert(storedSettings, db, SettingsKeys.AiModelsPath, request.Ai.ModelsPath, dateTimeProvider.UtcNow);
        Upsert(storedSettings, db, SettingsKeys.RuntimeDataPath, request.RuntimePaths.DataPath, dateTimeProvider.UtcNow);
        Upsert(storedSettings, db, SettingsKeys.RuntimeDatabasePath, request.RuntimePaths.DatabasePath, dateTimeProvider.UtcNow);
        Upsert(storedSettings, db, SettingsKeys.RuntimeFilesPath, request.RuntimePaths.FilesPath, dateTimeProvider.UtcNow);
        Upsert(storedSettings, db, SettingsKeys.RuntimeIndexPath, request.RuntimePaths.IndexPath, dateTimeProvider.UtcNow);
        Upsert(storedSettings, db, SettingsKeys.RuntimeLogsPath, request.RuntimePaths.LogsPath, dateTimeProvider.UtcNow);
        Upsert(storedSettings, db, SettingsKeys.SyncEnabled, request.Sync.Enabled.ToString(), dateTimeProvider.UtcNow);
        Upsert(storedSettings, db, SettingsKeys.SyncAutoSync, request.Sync.AutoSync.ToString(), dateTimeProvider.UtcNow);

        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }

    private static string GetString(IReadOnlyDictionary<string, string> settings, string key, string fallback)
    {
        return settings.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : fallback;
    }

    private static bool GetBool(IReadOnlyDictionary<string, string> settings, string key, bool fallback)
    {
        return settings.TryGetValue(key, out var value) && bool.TryParse(value, out var parsed)
            ? parsed
            : fallback;
    }

    private static void Upsert(
        IReadOnlyDictionary<string, AppSetting> storedSettings,
        AppDbContext db,
        string key,
        string value,
        DateTimeOffset now)
    {
        if (storedSettings.TryGetValue(key, out var setting))
        {
            setting.Value = value;
            setting.UpdatedAt = now;
            return;
        }

        db.AppSettings.Add(new AppSetting
        {
            Key = key,
            Value = value,
            CreatedAt = now,
        });
    }

    private static Dictionary<string, string[]> Validate(AppSettingsDto request)
    {
        var errors = new Dictionary<string, string[]>();

        if (!Enum.TryParse<AppTheme>(request.Appearance.Theme, ignoreCase: true, out _))
        {
            errors["appearance.theme"] = ["Theme must be Light, Dark, or System."];
        }

        if (!Enum.TryParse<AiProviderType>(request.Ai.Provider, ignoreCase: true, out _))
        {
            errors["ai.provider"] = ["AI provider must be Ollama or LlamaCpp."];
        }

        AddRequired(errors, "ai.chatModel", request.Ai.ChatModel);
        AddRequired(errors, "ai.embeddingModel", request.Ai.EmbeddingModel);
        AddRequired(errors, "ai.runtimePath", request.Ai.RuntimePath);
        AddRequired(errors, "ai.modelsPath", request.Ai.ModelsPath);
        AddRequired(errors, "runtimePaths.dataPath", request.RuntimePaths.DataPath);
        AddRequired(errors, "runtimePaths.databasePath", request.RuntimePaths.DatabasePath);
        AddRequired(errors, "runtimePaths.filesPath", request.RuntimePaths.FilesPath);
        AddRequired(errors, "runtimePaths.indexPath", request.RuntimePaths.IndexPath);
        AddRequired(errors, "runtimePaths.logsPath", request.RuntimePaths.LogsPath);

        return errors;
    }

    private static void AddRequired(Dictionary<string, string[]> errors, string key, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors[key] = ["Value is required."];
        }
    }

    private static class SettingsKeys
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
    }
}
