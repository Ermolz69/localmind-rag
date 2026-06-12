using System.Net;
using System.Net.Http.Json;
using KnowledgeApp.Contracts.Rag;
using KnowledgeApp.RagEvaluationTests.TestSupport;
using Xunit;

namespace KnowledgeApp.RagEvaluationTests;

public class HybridBaselineTests(RagEvaluationTestFactory factory) : IClassFixture<RagEvaluationTestFactory>
{
    private readonly RagEvaluationSeeder seeder = new(factory);

    [Fact]
    public async Task Semantic_Search_Should_Struggle_With_Exact_Keywords()
    {
        using HttpClient client = factory.CreateClient();
        await seeder.SeedAsync(client);

        // A highly specific exact keyword that semantic search typically struggles with
        // compared to BM25 if the term is rare or out-of-vocabulary for the dense model.
        string query = "XYZ-9982-Alpha";

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/search/semantic",
            new SemanticSearchRequest(query, Limit: 5));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        SemanticSearchResponse? results = await response.Content.ReadApiDataAsync<SemanticSearchResponse>();

        Assert.NotNull(results);

        // Note: In a pure semantic model, if "XYZ-9982-Alpha" is not semantically meaningful,
        // it might return unrelated documents or have a very low score.
        // This test serves as a documentation baseline for Hybrid Search.
        // When Hybrid Search (BM25 + RRF) is added, we expect this query to hit exactly the document containing it.

        // For now, we just ensure the endpoint doesn't crash and we document the baseline.
        // There is no assertion on the exact document returned here because we expect semantic search to struggle,
        // unless we seed a specific document containing "XYZ-9982-Alpha" and prove the score is low.
    }
}
