using System.Net;
using System.Net.Http.Json;

using KnowledgeApp.Contracts.Chats;
using KnowledgeApp.Contracts.Rag;
using KnowledgeApp.RagEvaluationTests.TestSupport;

namespace KnowledgeApp.RagEvaluationTests;

public sealed class ChatGroundingTests(
    RagEvaluationTestFactory factory) : IClassFixture<RagEvaluationTestFactory>
{
    private readonly RagEvaluationSeeder seeder = new(factory);

    [Fact]
    public async Task Chat_Answer_Should_Use_The_Retrieved_Expected_Document()
    {
        using HttpClient client = factory.CreateClient();

        await seeder.SeedAsync(client);

        foreach (RagEvaluationCase testCase in RagFixtureLoader.PositiveCases())
        {
            ConversationDto conversation =
                await CreateConversationAsync(client, testCase.Id);

            RagAnswerDto answer =
                await SendQuestionAsync(
                    client,
                    conversation.Id,
                    testCase.Question);

            RagSourceDto source = Assert.Single(answer.Sources);

            Assert.Equal(
                testCase.ExpectedDocument,
                source.DocumentName);

            foreach (string requiredTerm in testCase.RequiredAnswerTerms)
            {
                Assert.Contains(
                    requiredTerm,
                    answer.Answer,
                    StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public async Task Chat_Answer_Should_Not_Use_Unrelated_Fixture_Content()
    {
        using HttpClient client = factory.CreateClient();

        await seeder.SeedAsync(client);

        foreach (RagEvaluationCase testCase in RagFixtureLoader.PositiveCases())
        {
            ConversationDto conversation =
                await CreateConversationAsync(client, $"forbidden-{testCase.Id}");

            RagAnswerDto answer =
                await SendQuestionAsync(
                    client,
                    conversation.Id,
                    testCase.Question);

            foreach (string forbiddenTerm in testCase.ForbiddenTerms)
            {
                Assert.DoesNotContain(
                    forbiddenTerm,
                    answer.Answer,
                    StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    private static async Task<ConversationDto> CreateConversationAsync(
        HttpClient client,
        string caseId)
    {
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/chats",
            new CreateConversationRequest($"RAG evaluation: {caseId}"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        ConversationDto? conversation =
            await response.Content.ReadApiDataAsync<ConversationDto>();

        Assert.NotNull(conversation);

        return conversation;
    }

    private static async Task<RagAnswerDto> SendQuestionAsync(
        HttpClient client,
        Guid conversationId,
        string question)
    {
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/v1/chats/{conversationId}/messages",
            new ChatMessageRequest(question));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        RagAnswerDto? answer =
            await response.Content.ReadApiDataAsync<RagAnswerDto>();

        Assert.NotNull(answer);

        return answer;
    }
}
