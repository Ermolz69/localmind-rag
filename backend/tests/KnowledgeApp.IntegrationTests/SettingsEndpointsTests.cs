using System.Net;
using System.Net.Http.Json;
using KnowledgeApp.Contracts.Common;
using KnowledgeApp.Contracts.Settings;
using KnowledgeApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
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
        using HttpClient? client = factory.CreateClient();

        AppSettingsDto? settings = await client.GetApiDataAsync<AppSettingsDto>("/api/settings");

        Assert.NotNull(settings);
        Assert.NotNull(settings.Appearance);
        Assert.NotNull(settings.Ai);
        Assert.NotNull(settings.RuntimePaths);
        Assert.NotNull(settings.Sync);
        Assert.False(string.IsNullOrWhiteSpace(settings.Appearance.Theme));
        Assert.False(string.IsNullOrWhiteSpace(settings.Ai.Provider));
        Assert.False(string.IsNullOrWhiteSpace(settings.Ai.ChatModel));
        Assert.False(string.IsNullOrWhiteSpace(settings.Ai.EmbeddingModel));
        Assert.False(string.IsNullOrWhiteSpace(settings.RuntimePaths.DatabasePath));
    }

    [Fact]
    public async Task PutSettings_Should_Save_Settings_In_Sqlite()
    {
        using HttpClient? client = factory.CreateClient();

        AppSettingsDto? request = new AppSettingsDto(
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
                AutoSync: false));

        using HttpResponseMessage? response = await client.PutAsJsonAsync("/api/settings", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        AppSettingsDto? saved = await client.GetApiDataAsync<AppSettingsDto>("/api/settings");

        Assert.NotNull(saved);
        Assert.Equal("Dark", saved.Appearance.Theme);
        Assert.Equal("Ollama", saved.Ai.Provider);
        Assert.Equal("llama3.2", saved.Ai.ChatModel);
        Assert.True(saved.Sync.Enabled);
        Assert.False(saved.Sync.AutoSync);

        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        AppDbContext? db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Domain.Entities.AppSetting? storedTheme = await db.AppSettings.SingleAsync(x => x.Key == "App.Theme");
        Domain.Entities.AppSetting? storedProvider = await db.AppSettings.SingleAsync(x => x.Key == "Ai.Provider");
        Domain.Entities.AppSetting? storedSyncEnabled = await db.AppSettings.SingleAsync(x => x.Key == "Sync.Enabled");

        Assert.Equal("Dark", storedTheme.Value);
        Assert.Equal("Ollama", storedProvider.Value);
        Assert.Equal("True", storedSyncEnabled.Value);
    }

    [Fact]
    public async Task PutSettings_Should_Return_ProblemDetails_For_Invalid_Settings()
    {
        using HttpClient? client = factory.CreateClient();

        AppSettingsDto? request = new AppSettingsDto(
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
                AutoSync: false));

        using HttpResponseMessage? response = await client.PutAsJsonAsync("/api/settings", request);

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
                AutoSync: false));

        using HttpResponseMessage response = await client.PutAsJsonAsync("/api/settings", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        ApiResponse<object?> envelope = await response.Content.ReadApiErrorAsync();
        Assert.Equal("VALIDATION_FAILED", envelope.Error!.Code);
        Assert.Contains(envelope.Error.Details ?? [], detail => detail.Field == "ai.runtimePath");
        Assert.Contains(envelope.Error.Details ?? [], detail => detail.Field == "ai.modelsPath");
        Assert.Contains(envelope.Error.Details ?? [], detail => detail.Field == "runtimePaths.databasePath");
        Assert.Contains(envelope.Error.Details ?? [], detail => detail.Field == "runtimePaths.logsPath");
    }
}
