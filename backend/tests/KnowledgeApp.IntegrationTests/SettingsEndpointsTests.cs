using System.Net;
using System.Net.Http.Json;
using KnowledgeApp.Contracts.Settings;
using KnowledgeApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeApp.IntegrationTests;

public sealed class SettingsEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public SettingsEndpointsTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task GetSettings_Should_Return_Typed_Settings_Model()
    {
        using var client = factory.CreateClient();

        var settings = await client.GetFromJsonAsync<AppSettingsDto>("/api/settings");

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
        using var client = factory.CreateClient();

        var request = new AppSettingsDto(
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

        using var response = await client.PutAsJsonAsync("/api/settings", request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var saved = await client.GetFromJsonAsync<AppSettingsDto>("/api/settings");

        Assert.NotNull(saved);
        Assert.Equal("Dark", saved.Appearance.Theme);
        Assert.Equal("Ollama", saved.Ai.Provider);
        Assert.Equal("llama3.2", saved.Ai.ChatModel);
        Assert.True(saved.Sync.Enabled);
        Assert.False(saved.Sync.AutoSync);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var storedTheme = await db.AppSettings.SingleAsync(x => x.Key == "App.Theme");
        var storedProvider = await db.AppSettings.SingleAsync(x => x.Key == "Ai.Provider");
        var storedSyncEnabled = await db.AppSettings.SingleAsync(x => x.Key == "Sync.Enabled");

        Assert.Equal("Dark", storedTheme.Value);
        Assert.Equal("Ollama", storedProvider.Value);
        Assert.Equal("True", storedSyncEnabled.Value);
    }

    [Fact]
    public async Task PutSettings_Should_Return_ProblemDetails_For_Invalid_Settings()
    {
        using var client = factory.CreateClient();

        var request = new AppSettingsDto(
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

        using var response = await client.PutAsJsonAsync("/api/settings", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Contains("appearance.theme", problem.Errors.Keys);
        Assert.Contains("ai.provider", problem.Errors.Keys);
        Assert.Contains("ai.chatModel", problem.Errors.Keys);
    }
}
