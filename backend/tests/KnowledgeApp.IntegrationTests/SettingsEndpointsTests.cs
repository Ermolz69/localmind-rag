using System.Net;
using System.Net.Http.Json;
using KnowledgeApp.Contracts.Common;
using KnowledgeApp.Contracts.Settings;
using KnowledgeApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeApp.IntegrationTests;

public sealed class SettingsEndpointsTests : IClassFixture<LocalApiTestFactory>
{
    private readonly LocalApiTestFactory factory;

    public SettingsEndpointsTests(LocalApiTestFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task GetSettings_Should_Return_Typed_Settings_Model()
    {
        using HttpClient client = factory.CreateClient();

        AppSettingsDto? settings = await client.GetApiDataAsync<AppSettingsDto>("/api/v1/settings");

        Assert.NotNull(settings);
        Assert.NotNull(settings.Appearance);
        Assert.NotNull(settings.Ai);
        Assert.NotNull(settings.RuntimePaths);
        Assert.NotNull(settings.Sync);
        Assert.NotNull(settings.WatchedFolders);
        Assert.NotNull(settings.WatchedFolders.Folders);

        Assert.False(string.IsNullOrWhiteSpace(settings.Appearance.Theme));
        Assert.False(string.IsNullOrWhiteSpace(settings.Ai.Provider));
        Assert.False(string.IsNullOrWhiteSpace(settings.Ai.ChatModel));
        Assert.False(string.IsNullOrWhiteSpace(settings.Ai.EmbeddingModel));
        Assert.False(string.IsNullOrWhiteSpace(settings.RuntimePaths.DatabasePath));

        Assert.InRange(settings.WatchedFolders.DebounceMilliseconds, 250, 60000);
        Assert.Equal("MarkDeleted", settings.WatchedFolders.DeletePolicy);
    }

    [Fact]
    public async Task PutSettings_Should_Save_Settings_In_Sqlite()
    {
        string watchedPath = Path.Combine(
            Path.GetTempPath(),
            "localmind-settings-watch",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(watchedPath);

        try
        {
            using HttpClient client = factory.CreateClient();

            AppSettingsDto request = new AppSettingsDto(
                Appearance: new AppearanceSettingsDto("Dark"),
                Ai: new AiSettingsDto(
                    Provider: "Ollama",
                    ChatModel: "llama3.2",
                    EmbeddingModel: "nomic-embed-text",
                    RuntimePath: "runtime/ai/bin/ollama.exe",
                    ModelsPath: "runtime/ai/models"),
                RuntimePaths: new RuntimePathsSettingsDto(
                    DataPath: "runtime/app/data",
                    DatabasePath: "runtime/app/data/knowledge-app.db",
                    FilesPath: "runtime/app/files",
                    IndexPath: "runtime/app/indexes",
                    LogsPath: "runtime/app/logs"),
                Sync: new SyncSettingsDto(
                    Enabled: true,
                    AutoSync: false),
                WatchedFolders: new WatchedFoldersSettingsDto(
                    Enabled: true,
                    DebounceMilliseconds: 1500,
                    DeletePolicy: "MarkDeleted",
                    Folders:
                    [
                        new WatchedFolderDto(
                            Path: watchedPath,
                            Enabled: true,
                            IncludeSubdirectories: false)
                    ]));

            using HttpResponseMessage response = await client.PutAsJsonAsync("/api/v1/settings", request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            AppSettingsDto? saved = await client.GetApiDataAsync<AppSettingsDto>("/api/v1/settings");

            Assert.NotNull(saved);
            Assert.Equal("Dark", saved.Appearance.Theme);
            Assert.Equal("Ollama", saved.Ai.Provider);
            Assert.Equal("llama3.2", saved.Ai.ChatModel);
            Assert.True(saved.Sync.Enabled);
            Assert.False(saved.Sync.AutoSync);

            Assert.True(saved.WatchedFolders.Enabled);
            Assert.Equal(1500, saved.WatchedFolders.DebounceMilliseconds);
            Assert.Equal("MarkDeleted", saved.WatchedFolders.DeletePolicy);
            Assert.Single(saved.WatchedFolders.Folders);
            Assert.Equal(watchedPath, saved.WatchedFolders.Folders[0].Path);
            Assert.True(saved.WatchedFolders.Folders[0].Enabled);
            Assert.False(saved.WatchedFolders.Folders[0].IncludeSubdirectories);

            await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();

            AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            Domain.Entities.AppSetting storedTheme = await db.AppSettings.SingleAsync(x => x.Key == "App.Theme");
            Domain.Entities.AppSetting storedProvider = await db.AppSettings.SingleAsync(x => x.Key == "Ai.Provider");
            Domain.Entities.AppSetting storedSyncEnabled = await db.AppSettings.SingleAsync(x => x.Key == "Sync.Enabled");
            Domain.Entities.AppSetting storedWatchedFoldersEnabled =
                await db.AppSettings.SingleAsync(x => x.Key == "WatchedFolders.Enabled");
            Domain.Entities.AppSetting storedWatchedFoldersDebounce =
                await db.AppSettings.SingleAsync(x => x.Key == "WatchedFolders.DebounceMilliseconds");
            Domain.Entities.AppSetting storedWatchedFoldersDeletePolicy =
                await db.AppSettings.SingleAsync(x => x.Key == "WatchedFolders.DeletePolicy");
            Domain.Entities.AppSetting storedWatchedFoldersJson =
                await db.AppSettings.SingleAsync(x => x.Key == "WatchedFolders.FoldersJson");

            Assert.Equal("Dark", storedTheme.Value);
            Assert.Equal("Ollama", storedProvider.Value);
            Assert.Equal("True", storedSyncEnabled.Value);

            Assert.Equal("True", storedWatchedFoldersEnabled.Value);
            Assert.Equal("1500", storedWatchedFoldersDebounce.Value);
            Assert.Equal("MarkDeleted", storedWatchedFoldersDeletePolicy.Value);
            Assert.Contains("localmind-settings-watch", storedWatchedFoldersJson.Value, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(watchedPath))
            {
                try
                {
                    Directory.Delete(watchedPath, recursive: true);
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        }
    }

    [Fact]
    public async Task PutSettings_Should_Return_ProblemDetails_For_Invalid_Settings()
    {
        using HttpClient client = factory.CreateClient();

        AppSettingsDto request = new AppSettingsDto(
            Appearance: new AppearanceSettingsDto("Blue"),
            Ai: new AiSettingsDto(
                Provider: "UnknownProvider",
                ChatModel: "",
                EmbeddingModel: "nomic-embed-text",
                RuntimePath: "runtime/ai/bin/llama-server.exe",
                ModelsPath: "runtime/ai/models"),
            RuntimePaths: new RuntimePathsSettingsDto(
                DataPath: "runtime/app/data",
                DatabasePath: "runtime/app/data/knowledge-app.db",
                FilesPath: "runtime/app/files",
                IndexPath: "runtime/app/indexes",
                LogsPath: "runtime/app/logs"),
            Sync: new SyncSettingsDto(
                Enabled: false,
                AutoSync: false),
            WatchedFolders: CreateDefaultWatchedFoldersSettings());

        using HttpResponseMessage response = await client.PutAsJsonAsync("/api/v1/settings", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        ApiResponse<object?> envelope = await response.Content.ReadApiErrorAsync();

        Assert.Equal("VALIDATION_FAILED", envelope.Error!.Code);

        Assert.Contains(envelope.Error.Details ?? [], detail => detail.Field == "appearance.theme");
        Assert.Contains(envelope.Error.Details ?? [], detail => detail.Field == "ai.provider");
        Assert.Contains(envelope.Error.Details ?? [], detail => detail.Field == "ai.chatModel");
    }

    [Fact]
    public async Task PutSettings_Should_Return_Field_Errors_For_Missing_Runtime_And_Ai_Paths()
    {
        using HttpClient client = factory.CreateClient();

        AppSettingsDto request = new AppSettingsDto(
            Appearance: new AppearanceSettingsDto("System"),
            Ai: new AiSettingsDto(
                Provider: "LlamaCpp",
                ChatModel: "llama3.2",
                EmbeddingModel: "nomic-embed-text",
                RuntimePath: "",
                ModelsPath: ""),
            RuntimePaths: new RuntimePathsSettingsDto(
                DataPath: "",
                DatabasePath: "",
                FilesPath: "",
                IndexPath: "",
                LogsPath: ""),
            Sync: new SyncSettingsDto(
                Enabled: false,
                AutoSync: false),
            WatchedFolders: CreateDefaultWatchedFoldersSettings());

        using HttpResponseMessage response = await client.PutAsJsonAsync("/api/v1/settings", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        ApiResponse<object?> envelope = await response.Content.ReadApiErrorAsync();

        Assert.Equal("VALIDATION_FAILED", envelope.Error!.Code);

        Assert.Contains(envelope.Error.Details ?? [], detail => detail.Field == "ai.runtimePath");
        Assert.Contains(envelope.Error.Details ?? [], detail => detail.Field == "ai.modelsPath");
        Assert.Contains(envelope.Error.Details ?? [], detail => detail.Field == "runtimePaths.databasePath");
        Assert.Contains(envelope.Error.Details ?? [], detail => detail.Field == "runtimePaths.logsPath");
    }

    [Fact]
    public async Task PutSettings_Should_Return_Field_Error_For_Unsafe_Watched_Folder_Path()
    {
        using HttpClient client = factory.CreateClient();

        AppSettingsDto request = new AppSettingsDto(
            Appearance: new AppearanceSettingsDto("System"),
            Ai: new AiSettingsDto(
                Provider: "LlamaCpp",
                ChatModel: "llama3.2",
                EmbeddingModel: "nomic-embed-text",
                RuntimePath: "runtime/ai/bin/llama-server.exe",
                ModelsPath: "runtime/ai/models"),
            RuntimePaths: new RuntimePathsSettingsDto(
                DataPath: "runtime/app/data",
                DatabasePath: "runtime/app/data/knowledge-app.db",
                FilesPath: "runtime/app/files",
                IndexPath: "runtime/app/indexes",
                LogsPath: "runtime/app/logs"),
            Sync: new SyncSettingsDto(
                Enabled: false,
                AutoSync: false),
            WatchedFolders: new WatchedFoldersSettingsDto(
                Enabled: true,
                DebounceMilliseconds: 1000,
                DeletePolicy: "MarkDeleted",
                Folders:
                [
                    new WatchedFolderDto(
                        Path: "relative-watch-folder",
                        Enabled: true,
                        IncludeSubdirectories: false)
                ]));

        using HttpResponseMessage response = await client.PutAsJsonAsync("/api/v1/settings", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        ApiResponse<object?> envelope = await response.Content.ReadApiErrorAsync();

        Assert.Equal("VALIDATION_FAILED", envelope.Error!.Code);
        Assert.Contains(envelope.Error.Details ?? [], detail => detail.Field == "watchedFolders.folders[0].path");
    }

    private static WatchedFoldersSettingsDto CreateDefaultWatchedFoldersSettings()
    {
        return new WatchedFoldersSettingsDto(
            Enabled: false,
            DebounceMilliseconds: 1000,
            DeletePolicy: "MarkDeleted",
            Folders: []);
    }
}
