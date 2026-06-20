using System.Net;
using System.Net.Http.Json;

using KnowledgeApp.Contracts.Chats;
using KnowledgeApp.Contracts.Rag;

namespace KnowledgeApp.RagEvaluationTests.TestSupport;

internal static class ApiScenarioHelpers
{
    public static async Task<ConversationDto> CreateConversationAsync(
        HttpClient client,
        string? title = null)
    {
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/chats",
            new CreateConversationRequest(title ?? $"RAG evaluation {Guid.NewGuid():N}"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        ConversationDto? conversation = await response.Content.ReadApiDataAsync<ConversationDto>();

        Assert.NotNull(conversation);

        return conversation;
    }

    public static async Task<RagAnswerDto> SendChatMessageAsync(
        HttpClient client,
        Guid conversationId,
        string question)
    {
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/v1/chats/{conversationId}/messages",
            new ChatMessageRequest(question));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        RagAnswerDto? answer = await response.Content.ReadApiDataAsync<RagAnswerDto>();

        Assert.NotNull(answer);

        return answer;
    }
}
