using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Exceptions;
using KnowledgeApp.Infrastructure.Options;

using Microsoft.Extensions.Options;

namespace KnowledgeApp.Infrastructure.Services;

public sealed class LlamaCppEmbeddingGenerator(
    HttpClient httpClient,
    IOptions<RuntimeOptions> runtimeOptions,
    IOptions<EmbeddingOptions> embeddingOptions,
    EmbeddingModelCatalog catalog) : IBatchEmbeddingGenerator
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    private readonly RuntimeOptions runtime = runtimeOptions.Value;
    private readonly EmbeddingOptions embedding = embeddingOptions.Value;
    private readonly EmbeddingModelManifest manifest = catalog.GetDefault();

    public string ModelName => string.IsNullOrWhiteSpace(embedding.EmbeddingModel)
        ? manifest.ModelName
        : embedding.EmbeddingModel;

    public async Task<float[]> GenerateAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        IReadOnlyList<float[]> embeddings = await GenerateBatchAsync([text], cancellationToken);
        return embeddings[0];
    }

    public async Task<IReadOnlyList<float[]>> GenerateBatchAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
    {
        if (texts.Count == 0)
        {
            return [];
        }

        if (texts.Any(string.IsNullOrWhiteSpace))
        {
            throw CreateExternalDependencyException(
                "llama.cpp embeddings request cannot include empty input.");
        }

        Uri requestUri = BuildUri("/v1/embeddings");
        EmbeddingRequest request = new(ModelName, texts);
        using HttpResponseMessage response = await httpClient.PostAsJsonAsync(
            requestUri,
            request,
            SerializerOptions,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string body = await response.Content.ReadAsStringAsync(cancellationToken);

            throw CreateExternalDependencyException(
                $"llama.cpp embeddings request failed with status {(int)response.StatusCode}: {body}");
        }

        EmbeddingResponse? payload =
            await response.Content.ReadFromJsonAsync<EmbeddingResponse>(
                SerializerOptions,
                cancellationToken);

        IReadOnlyList<EmbeddingResponseData>? data = payload?.Data;

        if (data is null || data.Count != texts.Count)
        {
            throw CreateExternalDependencyException(
                $"llama.cpp embeddings response count mismatch. Expected {texts.Count}, got {data?.Count ?? 0}.");
        }

        List<float[]> results = new(data.Count);
        foreach (EmbeddingResponseData item in data.OrderBy(item => item.Index))
        {
            if (item.Index < 0 || item.Index >= texts.Count)
            {
                throw CreateExternalDependencyException(
                    $"llama.cpp embeddings response contained invalid index {item.Index}.");
            }

            if (item.Embedding.Length == 0)
            {
                throw CreateExternalDependencyException(
                    "llama.cpp embeddings response did not contain an embedding vector.");
            }

            if (item.Embedding.Length != manifest.Dimension)
            {
                throw CreateExternalDependencyException(
                    $"llama.cpp embeddings response dimension mismatch. Expected {manifest.Dimension}, got {item.Embedding.Length}.");
            }

            results.Add(item.Embedding);
        }

        if (results.Count != texts.Count)
        {
            throw CreateExternalDependencyException(
                $"llama.cpp embeddings response count mismatch. Expected {texts.Count}, got {results.Count}.");
        }

        return results;
    }

    private Uri BuildUri(string path)
    {
        Uri baseUri = new(
            runtime.BaseUrl.TrimEnd('/') + "/",
            UriKind.Absolute);

        return new Uri(baseUri, path.TrimStart('/'));
    }

    private static ExternalDependencyAppException CreateExternalDependencyException(
        string detail)
    {
        return new ExternalDependencyAppException(
            ErrorCodes.Runtime.ExternalDependencyUnavailable,
            $"{ErrorMessages.Runtime.ExternalDependencyUnavailable} {detail}");
    }

    private sealed record EmbeddingRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("input")] IReadOnlyList<string> Input);

    private sealed record EmbeddingResponse(
        [property: JsonPropertyName("data")]
        IReadOnlyList<EmbeddingResponseData> Data);

    private sealed record EmbeddingResponseData(
        [property: JsonPropertyName("index")] int Index,
        [property: JsonPropertyName("embedding")] float[] Embedding);
}
