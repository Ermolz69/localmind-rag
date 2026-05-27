using System.Net;
using System.Net.Http.Json;

using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Chats;
using KnowledgeApp.Contracts.Rag;
using KnowledgeApp.RagEvaluationTests.TestSupport;

using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeApp.RagEvaluationTests;

public sealed class NoRelevantContextTests(
    RagEvaluationTestFactory factory) : IClassFixture<RagEvaluationTestFactory>
{
    private readonly RagEvaluationSeeder seeder = new(factory);

    [Fact]
    public async Task Rag_Context_Should_Be_Empty_For_Unrelated_Question_When_Fixtures_Exist()
    {
        using HttpClient client = factory.CreateClient();

        await seeder.SeedAsync(client);

        RagEvaluationCase testCase = RagFixtureLoader.NoContextCase();

        await using AsyncServiceScope scope =
            factory.Services.CreateAsyncScope();

        IRagContextBuilder contextBuilder =
            scope.ServiceProvider.GetRequiredService<IRagContextBuilder>();

        RagContext context = await contextBuilder.BuildAsync(
            new RagContextRequest(
                Guid.NewGuid(),
                testCase.Question,
                Limit: 3));

        Assert.Empty(context.Sources);
        Assert.Equal(string.Empty, context.ContextText);

        foreach (string forbiddenTerm in testCase.ForbiddenTerms)
        {
            Assert.DoesNotContain(
                forbiddenTerm,
                context.ContextText,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Chat_Should_Return_No_Context_Message_For_Unrelated_Question_When_Fixtures_Exist()
    {
        using HttpClient client = factory.CreateClient();

        await seeder.SeedAsync(client);

        RagEvaluationCase testCase = RagFixtureLoader.NoContextCase();

        ConversationDto conversation =
            await CreateConversationAsync(client);

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/v1/chats/{conversation.Id}/messages",
            new ChatMessageRequest(testCase.Question));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        RagAnswerDto? answer =
            await response.Content.ReadApiDataAsync<RagAnswerDto>();

        Assert.NotNull(answer);
        Assert.Empty(answer.Sources);

        Assert.Equal(
            "No relevant local sources were found for this question.",
            answer.Answer);

        foreach (string forbiddenTerm in testCase.ForbiddenTerms)
        {
            Assert.DoesNotContain(
                forbiddenTerm,
                answer.Answer,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    private static async Task<ConversationDto> CreateConversationAsync(
        HttpClient client)
    {
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/chats",
            new CreateConversationRequest("RAG evaluation: no relevant context"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        ConversationDto? conversation =
            await response.Content.ReadApiDataAsync<ConversationDto>();

        Assert.NotNull(conversation);

        return conversation;
    }
}
