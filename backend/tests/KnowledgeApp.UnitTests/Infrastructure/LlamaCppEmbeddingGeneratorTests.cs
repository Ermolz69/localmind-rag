using System.Globalization;
using System.Net;
using System.Text;

using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Exceptions;
using KnowledgeApp.Infrastructure.Options;
using KnowledgeApp.Infrastructure.Services;

using Microsoft.Extensions.Options;

namespace KnowledgeApp.UnitTests.Infrastructure;

public sealed class LlamaCppEmbeddingGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_Should_Read_Embedding_From_OpenAi_Compatible_Response()
    {
        float[] expected = Enumerable.Range(0, 1024)
            .Select(value => value / 1024f)
            .ToArray();

        using HttpClient client = new(
            new JsonResponseHandler(CreateEmbeddingResponse(expected)));

        LlamaCppEmbeddingGenerator generator = CreateGenerator(client);

        float[] embedding = await generator.GenerateAsync("hello");

        Assert.Equal(expected.Length, embedding.Length);
        Assert.Equal(expected[42], embedding[42]);
    }

    [Fact]
    public async Task GenerateAsync_Should_Reject_Unexpected_Dimension()
    {
        using HttpClient client = new(
            new JsonResponseHandler(CreateEmbeddingResponse([0.1f, 0.2f])));

        LlamaCppEmbeddingGenerator generator = CreateGenerator(client);

        ExternalDependencyAppException exception =
            await Assert.ThrowsAsync<ExternalDependencyAppException>(
                () => generator.GenerateAsync("hello"));

        Assert.Equal(
            ErrorCodes.Runtime.ExternalDependencyUnavailable,
            exception.Code);

        Assert.Contains(
            "dimension mismatch",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    private static LlamaCppEmbeddingGenerator CreateGenerator(HttpClient client)
    {
        IOptions<RuntimeOptions> runtimeOptions = Options.Create(new RuntimeOptions
        {
            BaseUrl = "http://127.0.0.1:11435",
        });

        IOptions<EmbeddingOptions> embeddingOptions =
            Options.Create(new EmbeddingOptions
            {
                EmbeddingModel = "bge-m3",
            });

        return new LlamaCppEmbeddingGenerator(
            client,
            runtimeOptions,
            embeddingOptions,
            new EmbeddingModelCatalog(embeddingOptions));
    }

    private static string CreateEmbeddingResponse(float[] embedding)
    {
        string values = string.Join(
            ",",
            embedding.Select(value =>
                value.ToString(CultureInfo.InvariantCulture)));

        return $$"""
            {
              "object": "list",
              "data": [
                {
                  "object": "embedding",
                  "index": 0,
                  "embedding": [{{values}}]
                }
              ],
              "model": "bge-m3"
            }
            """;
    }

    private sealed class JsonResponseHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Assert.Equal(HttpMethod.Post, request.Method);

            Assert.Equal(
                "http://127.0.0.1:11435/v1/embeddings",
                request.RequestUri?.ToString());

            HttpResponseMessage response = new(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json"),
            };

            return Task.FromResult(response);
        }
    }
}
