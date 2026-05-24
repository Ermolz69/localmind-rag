using System.Net;
using System.Net.Http.Json;
using KnowledgeApp.Contracts.Buckets;
using KnowledgeApp.Contracts.Common;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace KnowledgeApp.IntegrationTests;

public sealed class LocalApiSecurityTests(LocalApiTestFactory factory) : IClassFixture<LocalApiTestFactory>
{
    [Fact]
    public async Task Mutating_Endpoint_Should_Require_Local_Token_When_Configured()
    {
        using WebApplicationFactory<Program> secureFactory = CreateSecureFactory();
        using HttpClient client = secureFactory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync("/api/buckets", new CreateBucketRequest("Secure", null));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        ApiResponse<object?> envelope = await response.Content.ReadApiErrorAsync();
        Assert.Equal("LOCAL_TOKEN_REQUIRED", envelope.Error?.Code);
    }

    [Fact]
    public async Task Mutating_Endpoint_Should_Accept_Configured_Local_Token()
    {
        using WebApplicationFactory<Program> secureFactory = CreateSecureFactory();
        using HttpClient client = secureFactory.CreateClient();
        using HttpRequestMessage request = new(HttpMethod.Post, "/api/buckets")
        {
            Content = JsonContent.Create(new CreateBucketRequest("Secure", null)),
        };
        request.Headers.Add("X-LocalMind-Token", "test-token");

        using HttpResponseMessage response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private WebApplicationFactory<Program> CreateSecureFactory()
    {
        return factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["LocalApi:Security:Token"] = "test-token",
                })));
    }
}
