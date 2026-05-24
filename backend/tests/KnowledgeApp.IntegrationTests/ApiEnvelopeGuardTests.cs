using System.Net;
using System.Text.Json;
using KnowledgeApp.Contracts.Buckets;
using KnowledgeApp.Contracts.Common;
using KnowledgeApp.Contracts.Settings;

namespace KnowledgeApp.IntegrationTests;

public sealed class ApiEnvelopeGuardTests(LocalApiTestFactory factory) : IClassFixture<LocalApiTestFactory>
{
    private readonly HttpClient client = factory.CreateClient();

    [Theory]
    [InlineData("/api/settings")]
    [InlineData("/api/buckets")]
    public async Task Representative_Get_Endpoints_Should_Return_ApiResponse_Envelope(string path)
    {
        using HttpResponseMessage response = await client.GetAsync(path);
        response.EnsureSuccessStatusCode();

        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement root = document.RootElement;

        Assert.True(root.TryGetProperty("success", out JsonElement success));
        Assert.True(success.GetBoolean());
        Assert.True(root.TryGetProperty("data", out _));
        Assert.True(root.TryGetProperty("error", out JsonElement error));
        Assert.Equal(JsonValueKind.Null, error.ValueKind);
        AssertMetadata(root);
    }

    [Fact]
    public async Task Error_Responses_Should_Return_ApiResponse_Envelope_Not_ProblemDetails()
    {
        using HttpResponseMessage response = await client.GetAsync($"/api/documents/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement root = document.RootElement;

        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("data").ValueKind);
        Assert.Equal("DOCUMENT_NOT_FOUND", root.GetProperty("error").GetProperty("code").GetString());
        AssertMetadata(root);
        Assert.False(root.TryGetProperty("title", out _));
        Assert.False(root.TryGetProperty("detail", out _));
        Assert.False(root.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task Health_Should_Remain_Raw_Json()
    {
        using HttpResponseMessage response = await client.GetAsync("/api/health");
        response.EnsureSuccessStatusCode();

        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.True(document.RootElement.TryGetProperty("status", out _));
        Assert.False(document.RootElement.TryGetProperty("success", out _));
    }

    private static void AssertMetadata(JsonElement root)
    {
        JsonElement metadata = root.GetProperty("metadata");
        Assert.False(string.IsNullOrWhiteSpace(metadata.GetProperty("timestamp").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(metadata.GetProperty("requestId").GetString()));
    }
}
