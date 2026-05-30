using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using KnowledgeApp.Contracts.Chats;
using KnowledgeApp.Contracts.Common;
using KnowledgeApp.Contracts.Rag;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeApp.IntegrationTests;

public sealed class ChatStreamApiTests : IClassFixture<LocalApiTestFactory>
{
    private readonly LocalApiTestFactory factory;

    public ChatStreamApiTests(LocalApiTestFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task StreamChatMessage_Should_Return_SSE_Stream()
    {
        // 1. Create a chat
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage createResponse = await client.PostAsJsonAsync("/api/v1/chats", new CreateConversationRequest("Test Stream"));
        createResponse.EnsureSuccessStatusCode();
        ConversationDto? chat = await createResponse.Content.ReadApiDataAsync<ConversationDto>();

        Assert.NotNull(chat);

        // 2. Send streaming message
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/v1/chats/{chat.Id}/messages/stream",
            new ChatMessageRequest("Hello stream"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);

        using Stream stream = await response.Content.ReadAsStreamAsync();
        using StreamReader reader = new(stream);

        List<RagAnswerChunkDto> chunks = [];
        while (await reader.ReadLineAsync() is { } line)
        {
            if (line.StartsWith("data: "))
            {
                string data = line["data: ".Length..];
                var chunk = JsonSerializer.Deserialize<RagAnswerChunkDto>(data, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                if (chunk != null)
                {
                    chunks.Add(chunk);
                }
            }
        }

        Assert.NotEmpty(chunks);
        Assert.Contains(chunks, c => !string.IsNullOrEmpty(c.Text));
    }

    [Fact]
    public async Task StreamChatMessage_Should_Return_BadRequest_When_Message_Is_Empty()
    {
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage createResponse = await client.PostAsJsonAsync("/api/v1/chats", new CreateConversationRequest("Test Stream Bad Request"));
        createResponse.EnsureSuccessStatusCode();
        ConversationDto? chat = await createResponse.Content.ReadApiDataAsync<ConversationDto>();
        Assert.NotNull(chat);

        // Send empty message
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/v1/chats/{chat.Id}/messages/stream",
            new ChatMessageRequest("   "));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        ApiResponse<object?>? apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<object?>>();
        Assert.NotNull(apiResponse);
        Assert.False(apiResponse.Success);
        Assert.Equal("VALIDATION_FAILED", apiResponse.Error?.Code);
        Assert.Contains(apiResponse.Error?.Details ?? [], detail => detail.Field == "content");
    }

    [Fact]
    public async Task StreamChatMessage_Should_Return_NotFound_When_Conversation_Does_Not_Exist()
    {
        using HttpClient client = factory.CreateClient();
        Guid nonExistentId = Guid.NewGuid();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/v1/chats/{nonExistentId}/messages/stream",
            new ChatMessageRequest("Hello stream not found"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        ApiResponse<object?>? apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<object?>>();
        Assert.NotNull(apiResponse);
        Assert.False(apiResponse.Success);
        Assert.Equal("CHAT_NOT_FOUND", apiResponse.Error?.Code);
    }

    [Fact]
    public async Task StreamChatMessage_Should_Save_Partial_Response_When_Cancelled()
    {
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage createResponse = await client.PostAsJsonAsync("/api/v1/chats", new CreateConversationRequest("Test Cancel Stream"));
        createResponse.EnsureSuccessStatusCode();
        ConversationDto? chat = await createResponse.Content.ReadApiDataAsync<ConversationDto>();
        Assert.NotNull(chat);

        using var cts = new CancellationTokenSource();
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/v1/chats/{chat.Id}/messages/stream",
            new ChatMessageRequest("Hello stream cancel"),
            cts.Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);

        using Stream stream = await response.Content.ReadAsStreamAsync();
        using StreamReader reader = new(stream);

        List<RagAnswerChunkDto> chunks = [];
        try
        {
            while (await reader.ReadLineAsync() is { } line)
            {
                if (line.StartsWith("data: "))
                {
                    string data = line["data: ".Length..];
                    var chunk = JsonSerializer.Deserialize<RagAnswerChunkDto>(data, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                    if (chunk != null)
                    {
                        chunks.Add(chunk);
                    }
                }
            }
        }
        catch (Exception ex) when (ex is OperationCanceledException or IOException)
        {
            // Expected exception due to cancellation
        }

        // Wait a short time for the background database save to complete (uses CancellationToken.None)
        await Task.Delay(100);

        // Verify database contains the user message and partial assistant message
        await using AsyncServiceScope verificationScope = factory.Services.CreateAsyncScope();
        AppDbContext db = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();

        var messages = (await db.ChatMessages
            .Where(m => m.ConversationId == chat.Id)
            .ToListAsync())
            .OrderBy(m => m.CreatedAt)
            .ToList();

        // Should have user message and assistant message
        Assert.Equal(2, messages.Count);
        Assert.Equal(KnowledgeApp.Domain.Enums.ChatRole.User, messages[0].Role);
        Assert.Equal(KnowledgeApp.Domain.Enums.ChatRole.Assistant, messages[1].Role);

        // Assistant content should contain some but not all of the tokens
        Assert.NotEmpty(messages[1].Content);
        int expectedFullResponseLength = 341;
        Assert.True(messages[1].Content.Length < expectedFullResponseLength, $"Content length {messages[1].Content.Length} should be less than full length {expectedFullResponseLength}");
    }

    [Fact]
    public async Task StreamChatMessage_Should_Handle_MidStream_Failures_Consistently()
    {
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage createResponse = await client.PostAsJsonAsync("/api/v1/chats", new CreateConversationRequest("Test Stream MidStream Error"));
        createResponse.EnsureSuccessStatusCode();
        ConversationDto? chat = await createResponse.Content.ReadApiDataAsync<ConversationDto>();
        Assert.NotNull(chat);

        // Send a question containing the simulated error trigger
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/v1/chats/{chat.Id}/messages/stream",
            new ChatMessageRequest("trigger-error"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);

        using Stream stream = await response.Content.ReadAsStreamAsync();
        using StreamReader reader = new(stream);

        ApiResponse<object?>? errorResponseEnvelope = null;
        while (await reader.ReadLineAsync() is { } line)
        {
            if (line.StartsWith("data: "))
            {
                string data = line["data: ".Length..];
                var envelope = JsonSerializer.Deserialize<ApiResponse<object?>>(data, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                if (envelope != null && !envelope.Success)
                {
                    errorResponseEnvelope = envelope;
                    break;
                }
            }
        }

        Assert.NotNull(errorResponseEnvelope);
        Assert.False(errorResponseEnvelope.Success);
        Assert.Equal("INTERNAL_SERVER_ERROR", errorResponseEnvelope.Error?.Code);
        Assert.Equal("An unexpected error occurred.", errorResponseEnvelope.Error?.Message);
    }
}
