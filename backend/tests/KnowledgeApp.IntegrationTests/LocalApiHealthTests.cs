using System.Net;
using System.Text.Json;

namespace KnowledgeApp.IntegrationTests;

public sealed class LocalApiHealthTests : IClassFixture<LocalApiTestFactory>
{
    private readonly LocalApiTestFactory factory;

    public LocalApiHealthTests(LocalApiTestFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task HealthEndpoint_Should_Return_Ok()
    {
        using HttpClient? client = factory.CreateClient();

        using HttpResponseMessage? response = await client.GetAsync("/api/health", CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CorsPreflight_Should_Allow_Tauri_Localhost_Origin()
    {
        using HttpClient? client = factory.CreateClient();
        using HttpRequestMessage? request = new HttpRequestMessage(HttpMethod.Options, "/api/buckets");
        request.Headers.Add("Origin", "http://tauri.localhost");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        using HttpResponseMessage? response = await client.SendAsync(request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out IEnumerable<string>? values));
        Assert.Contains("http://tauri.localhost", values);
    }

    [Fact]
    public async Task OpenApiDocument_Should_Describe_Main_LocalApi_Contracts()
    {
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/openapi/v1.json", CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using Stream stream = await response.Content.ReadAsStreamAsync(CancellationToken.None);
        using JsonDocument document = await JsonDocument.ParseAsync(stream, cancellationToken: CancellationToken.None);
        JsonElement root = document.RootElement;
        JsonElement paths = root.GetProperty("paths");

        Assert.True(paths.TryGetProperty("/api/documents", out _));
        Assert.True(paths.TryGetProperty("/api/chats/{id}/messages", out _));
        Assert.True(paths.TryGetProperty("/api/search/semantic", out JsonElement semanticSearchPath));
        Assert.True(semanticSearchPath.GetProperty("post").TryGetProperty("summary", out JsonElement summary));
        Assert.Equal("Runs semantic search.", summary.GetString());

        string openApiJson = root.GetRawText();
        Assert.Contains("CreateBucketRequest", openApiJson, StringComparison.Ordinal);
        Assert.Contains("DocumentDto", openApiJson, StringComparison.Ordinal);
        Assert.Contains("SemanticSearchRequest", openApiJson, StringComparison.Ordinal);
        Assert.Contains("Generated RAG answer with source citations.", openApiJson, StringComparison.Ordinal);
    }
}
