using System.Net;
using System.Net.Http.Json;

using KnowledgeApp.Contracts.Rag;
using KnowledgeApp.RagEvaluationTests.TestSupport;

namespace KnowledgeApp.RagEvaluationTests;

public sealed class RetrievalQualityTests(
    RagEvaluationTestFactory factory) : IClassFixture<RagEvaluationTestFactory>
{
    private readonly RagEvaluationSeeder seeder = new(factory);

    [Fact]
    public async Task Semantic_Search_Should_Rank_Expected_Fixture_Document_First()
    {
        using HttpClient client = factory.CreateClient();

        await seeder.SeedAsync(client);

        foreach (RagEvaluationCase testCase in RagFixtureLoader.PositiveCases())
        {
            using HttpResponseMessage response = await client.PostAsJsonAsync(
                "/api/v1/search/semantic",
                new SemanticSearchRequest(
                    testCase.Question,
                    Limit: 3));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            SemanticSearchResponse? results =
                await response.Content.ReadApiDataAsync<SemanticSearchResponse>();

            Assert.NotNull(results);
            Assert.NotEmpty(results.Sources);

            RagSourceDto topSource = results.Sources[0];

            Assert.Equal(
                testCase.ExpectedDocument,
                topSource.DocumentName);

            Assert.True(
                topSource.Score > 0.99,
                $"Expected '{testCase.ExpectedDocument}' to be an exact controlled match for case '{testCase.Id}', but score was {topSource.Score}.");
        }
    }

    [Fact]
    public async Task Semantic_Search_Should_Not_Rank_Unrelated_Document_Above_Expected_Document()
    {
        using HttpClient client = factory.CreateClient();

        await seeder.SeedAsync(client);

        foreach (RagEvaluationCase testCase in RagFixtureLoader.PositiveCases())
        {
            SemanticSearchResponse? results =
                await PostSearchAsync(client, testCase.Question);

            Assert.NotNull(results);

            int expectedIndex = Array.FindIndex(
                results.Sources.ToArray(),
                source => string.Equals(
                    source.DocumentName,
                    testCase.ExpectedDocument,
                    StringComparison.OrdinalIgnoreCase));

            Assert.Equal(0, expectedIndex);

            foreach (string forbiddenTerm in testCase.ForbiddenTerms)
            {
                Assert.DoesNotContain(
                    forbiddenTerm,
                    results.Sources[0].Snippet,
                    StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    private static async Task<SemanticSearchResponse?> PostSearchAsync(
        HttpClient client,
        string question)
    {
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/search/semantic",
            new SemanticSearchRequest(question, Limit: 3));

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadApiDataAsync<SemanticSearchResponse>();
    }
}
