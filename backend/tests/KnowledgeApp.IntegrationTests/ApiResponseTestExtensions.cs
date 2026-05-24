using System.Net.Http.Json;
using KnowledgeApp.Contracts.Common;

namespace KnowledgeApp.IntegrationTests;

internal static class ApiResponseTestExtensions
{
    public static async Task<T?> GetApiDataAsync<T>(this HttpClient client, string requestUri)
    {
        using HttpResponseMessage response = await client.GetAsync(requestUri);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadApiDataAsync<T>();
    }

    public static async Task<T?> ReadApiDataAsync<T>(this HttpContent content)
    {
        ApiResponse<T>? envelope = await content.ReadFromJsonAsync<ApiResponse<T>>();
        Assert.NotNull(envelope);
        Assert.True(envelope.Success);
        Assert.NotNull(envelope.Metadata);
        Assert.False(string.IsNullOrWhiteSpace(envelope.Metadata.RequestId));
        return envelope.Data;
    }

    public static async Task<ApiResponse<object?>> ReadApiErrorAsync(this HttpContent content)
    {
        ApiResponse<object?>? envelope = await content.ReadFromJsonAsync<ApiResponse<object?>>();
        Assert.NotNull(envelope);
        Assert.False(envelope.Success);
        Assert.Null(envelope.Data);
        Assert.NotNull(envelope.Error);
        Assert.NotNull(envelope.Metadata);
        Assert.False(string.IsNullOrWhiteSpace(envelope.Metadata.RequestId));
        return envelope;
    }
}
