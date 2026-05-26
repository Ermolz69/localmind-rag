using System.Net;
using System.Text.Json;

namespace KnowledgeApp.IntegrationTests;

public sealed class ApiVersioningTests : IClassFixture<LocalApiTestFactory>
{
    private readonly LocalApiTestFactory factory;

    public ApiVersioningTests(LocalApiTestFactory factory)
    {
        this.factory = factory;
    }

    [Theory]
    [InlineData("/api/v1/health")]
    [InlineData("/api/v1/diagnostics")]
    [InlineData("/api/v1/runtime/status")]
    [InlineData("/api/v1/buckets")]
    [InlineData("/api/v1/documents")]
    [InlineData("/api/v1/notes")]
    [InlineData("/api/v1/chats")]
    [InlineData("/api/v1/settings")]
    [InlineData("/api/v1/sync/status")]
    public async Task Public_Get_Endpoints_Should_Be_Available_Under_V1(string path)
    {
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response =
            await client.GetAsync(path, CancellationToken.None);

        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/health")]
    [InlineData("/api/diagnostics")]
    [InlineData("/api/runtime/status")]
    [InlineData("/api/buckets")]
    [InlineData("/api/documents")]
    [InlineData("/api/notes")]
    [InlineData("/api/chats")]
    [InlineData("/api/settings")]
    [InlineData("/api/sync/status")]
    public async Task Legacy_Unversioned_Endpoints_Should_Return_NotFound(string path)
    {
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response =
            await client.GetAsync(path, CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task OpenApi_Should_Expose_Versioned_Public_Api_Paths()
    {
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response =
            await client.GetAsync("/openapi/v1.json", CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using Stream stream =
            await response.Content.ReadAsStreamAsync(CancellationToken.None);

        using JsonDocument document =
            await JsonDocument.ParseAsync(stream, cancellationToken: CancellationToken.None);

        JsonElement paths = document.RootElement.GetProperty("paths");

        Assert.True(paths.TryGetProperty("/api/v1/health", out _));
        Assert.True(paths.TryGetProperty("/api/v1/diagnostics", out _));
        Assert.True(paths.TryGetProperty("/api/v1/documents", out _));
        Assert.True(paths.TryGetProperty("/api/v1/search/semantic", out _));
        Assert.True(paths.TryGetProperty("/api/v1/settings", out _));

        Assert.False(paths.TryGetProperty("/api/health", out _));
        Assert.False(paths.TryGetProperty("/api/diagnostics", out _));
        Assert.False(paths.TryGetProperty("/api/documents", out _));
        Assert.False(paths.TryGetProperty("/api/search/semantic", out _));
        Assert.False(paths.TryGetProperty("/api/settings", out _));
    }
}
