using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Rag;
using KnowledgeApp.RagEvaluationTests.TestSupport;

using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeApp.RagEvaluationTests;

public sealed class RetrievedContextTests(
    RagEvaluationTestFactory factory) : IClassFixture<RagEvaluationTestFactory>
{
    private readonly RagEvaluationSeeder seeder = new(factory);

    [Fact]
    public async Task Rag_Context_Should_Contain_Required_Terms_From_Expected_Document()
    {
        using HttpClient client = factory.CreateClient();

        await seeder.SeedAsync(client);

        await using AsyncServiceScope scope =
            factory.Services.CreateAsyncScope();

        IRagContextBuilder contextBuilder =
            scope.ServiceProvider.GetRequiredService<IRagContextBuilder>();

        foreach (RagEvaluationCase testCase in RagFixtureLoader.PositiveCases())
        {
            RagContext context = await contextBuilder.BuildAsync(
                new RagContextRequest(
                    Guid.NewGuid(),
                    testCase.Question,
                    Limit: 3));

            Assert.NotEmpty(context.Sources);

            Assert.Equal(
                testCase.ExpectedDocument,
                context.Sources[0].DocumentName);

            foreach (string requiredTerm in testCase.RequiredContextTerms)
            {
                Assert.Contains(
                    requiredTerm,
                    context.ContextText,
                    StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public async Task Rag_Context_Should_Not_Contain_Terms_From_Unrelated_Documents()
    {
        using HttpClient client = factory.CreateClient();

        await seeder.SeedAsync(client);

        await using AsyncServiceScope scope =
            factory.Services.CreateAsyncScope();

        IRagContextBuilder contextBuilder =
            scope.ServiceProvider.GetRequiredService<IRagContextBuilder>();

        foreach (RagEvaluationCase testCase in RagFixtureLoader.PositiveCases())
        {
            RagContext context = await contextBuilder.BuildAsync(
                new RagContextRequest(
                    Guid.NewGuid(),
                    testCase.Question,
                    Limit: 3));

            foreach (string forbiddenTerm in testCase.ForbiddenTerms)
            {
                Assert.DoesNotContain(
                    forbiddenTerm,
                    context.ContextText,
                    StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public async Task Rag_Context_Should_Include_Only_Relevant_Source_After_Score_Filtering()
    {
        using HttpClient client = factory.CreateClient();

        await seeder.SeedAsync(client);

        await using AsyncServiceScope scope =
            factory.Services.CreateAsyncScope();

        IRagContextBuilder contextBuilder =
            scope.ServiceProvider.GetRequiredService<IRagContextBuilder>();

        foreach (RagEvaluationCase testCase in RagFixtureLoader.PositiveCases())
        {
            RagContext context = await contextBuilder.BuildAsync(
                new RagContextRequest(
                    Guid.NewGuid(),
                    testCase.Question,
                    Limit: 3));

            RagSourceDto source = Assert.Single(context.Sources);

            Assert.Equal(
                testCase.ExpectedDocument,
                source.DocumentName);

            Assert.True(
                source.Score >= 0.8,
                $"Expected source for case '{testCase.Id}' to pass the configured RAG threshold.");
        }
    }
}
