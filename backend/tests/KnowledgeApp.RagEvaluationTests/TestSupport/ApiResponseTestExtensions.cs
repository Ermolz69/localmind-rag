using System.Net.Http.Json;

using KnowledgeApp.Contracts.Common;

namespace KnowledgeApp.RagEvaluationTests.TestSupport;

internal static class ApiResponseTestExtensions
{
    public static async Task<T?> GetApiDataAsync<T>(
        this HttpClient client,
        string requestUri)
    {
        using HttpResponseMessage response =
            await client.GetAsync(requestUri);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadApiDataAsync<T>();
    }

    public static async Task<T?> ReadApiDataAsync<T>(
        this HttpContent content)
    {
        ApiResponse<T>? envelope =
            await content.ReadFromJsonAsync<ApiResponse<T>>();

        Assert.NotNull(envelope);
        Assert.True(envelope.Success);
        Assert.NotNull(envelope.Metadata);
        Assert.False(string.IsNullOrWhiteSpace(envelope.Metadata.RequestId));

        return envelope.Data;
    }
}
