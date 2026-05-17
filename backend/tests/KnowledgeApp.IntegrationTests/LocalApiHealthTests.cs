using System.Net;

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
}
