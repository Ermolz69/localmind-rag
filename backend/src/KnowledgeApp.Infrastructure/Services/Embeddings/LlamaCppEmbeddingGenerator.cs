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
    IOptions<AiOptions> options,
    EmbeddingModelCatalog catalog) : IEmbeddingGenerator
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly AiOptions options = options.Value;
    private readonly EmbeddingModelManifest manifest = catalog.GetDefault();

    public string ModelName => string.IsNullOrWhiteSpace(options.EmbeddingModel)
        ? manifest.ModelName
        : options.EmbeddingModel;

    public async Task<float[]> GenerateAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        Uri requestUri = BuildUri("/v1/embeddings");
        EmbeddingRequest request = new(ModelName, text);
        using HttpResponseMessage response = await httpClient.PostAsJsonAsync(requestUri, request, SerializerOptions, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw CreateExternalDependencyException($"llama.cpp embeddings request failed with status {(int)response.StatusCode}: {body}");
        }

        EmbeddingResponse? payload = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(SerializerOptions, cancellationToken);
        float[]? embedding = payload?.Data.FirstOrDefault()?.Embedding;

        if (embedding is null || embedding.Length == 0)
        {
            throw CreateExternalDependencyException("llama.cpp embeddings response did not contain an embedding vector.");
        }

        if (embedding.Length != manifest.Dimension)
        {
            throw CreateExternalDependencyException($"llama.cpp embeddings response dimension mismatch. Expected {manifest.Dimension}, got {embedding.Length}.");
        }

        return embedding;
    }

    private Uri BuildUri(string path)
    {
        Uri baseUri = new(options.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
        return new Uri(baseUri, path.TrimStart('/'));
    }

    private static ExternalDependencyAppException CreateExternalDependencyException(string detail)
    {
        return new ExternalDependencyAppException(
            ErrorCodes.Runtime.ExternalDependencyUnavailable,
            $"{ErrorMessages.Runtime.ExternalDependencyUnavailable} {detail}");
    }

    private sealed record EmbeddingRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("input")] string Input);

    private sealed record EmbeddingResponse(
        [property: JsonPropertyName("data")] IReadOnlyList<EmbeddingResponseData> Data);

    private sealed record EmbeddingResponseData(
        [property: JsonPropertyName("embedding")] float[] Embedding);
}
