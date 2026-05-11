using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace KnowledgeApp.IntegrationTests;

public sealed class LocalApiHealthTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public LocalApiHealthTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task HealthEndpoint_Should_Return_Ok()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/health", CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
