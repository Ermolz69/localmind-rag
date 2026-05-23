using System.Net;
using System.Net.Http.Json;
using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Chats;
using KnowledgeApp.Contracts.Documents;
using KnowledgeApp.Contracts.Rag;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Persistence;
using KnowledgeApp.IntegrationTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeApp.IntegrationTests;

public sealed class ChatRagApiTests
{
    [Fact]
    public async Task SendChatMessage_Should_Search_Chunks_Save_Messages_And_Return_Sources()
    {
        using LocalApiTestFactory factory = new();
        using HttpClient client = factory.CreateClient();
        string documentText = $"Localmind RAG test source {Guid.NewGuid():N}";
        UploadDocumentResponse upload = await ApiScenarioHelpers.UploadTextDocumentAsync(client, documentText, $"rag-chat-{Guid.NewGuid():N}.txt");

        await using (AsyncServiceScope ingestionScope = factory.Services.CreateAsyncScope())
        {
            IIngestionJobProcessor processor = ingestionScope.ServiceProvider.GetRequiredService<IIngestionJobProcessor>();
            await processor.ProcessAsync(upload.IngestionJobId);
        }

        ConversationDto conversation = await ApiScenarioHelpers.CreateConversationAsync(client, "RAG chat");

        HttpResponseMessage sendResponse = await client.PostAsJsonAsync(
            $"/api/chats/{conversation.Id}/messages",
            new ChatMessageRequest(documentText));

        Assert.Equal(HttpStatusCode.OK, sendResponse.StatusCode);
        RagAnswerDto? answer = await sendResponse.Content.ReadFromJsonAsync<RagAnswerDto>();
        Assert.NotNull(answer);
        Assert.NotEmpty(answer.Answer);
        Assert.NotEmpty(answer.Sources);
        Assert.Contains(documentText, answer.Answer, StringComparison.Ordinal);
        Assert.Contains(answer.Sources, source =>
            source.DocumentId == upload.DocumentId &&
            source.ChunkId != Guid.Empty &&
            source.Score > 0 &&
            source.Snippet.Contains(documentText, StringComparison.Ordinal));

        await using AsyncServiceScope verificationScope = factory.Services.CreateAsyncScope();
        AppDbContext db = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Domain.Entities.ChatMessage[] messages = await db.ChatMessages
            .Where(message => message.ConversationId == conversation.Id)
            .ToArrayAsync();

        Assert.Contains(messages, message => message.Role == ChatRole.User && message.Content == documentText);
        Assert.Contains(messages, message => message.Role == ChatRole.Assistant && message.Content == answer.Answer);
    }

    [Fact]
    public async Task SendChatMessage_Should_Save_No_Source_Assistant_Message_When_No_Chunks_Exist()
    {
        using LocalApiTestFactory factory = new();
        using HttpClient client = factory.CreateClient();
        ConversationDto conversation = await ApiScenarioHelpers.CreateConversationAsync(client);

        HttpResponseMessage sendResponse = await client.PostAsJsonAsync(
            $"/api/chats/{conversation.Id}/messages",
            new ChatMessageRequest($"unknown topic {Guid.NewGuid():N}"));

        Assert.Equal(HttpStatusCode.OK, sendResponse.StatusCode);
        RagAnswerDto? answer = await sendResponse.Content.ReadFromJsonAsync<RagAnswerDto>();
        Assert.NotNull(answer);
        Assert.Empty(answer.Sources);
        Assert.Equal("No relevant local sources were found for this question.", answer.Answer);

        await using AsyncServiceScope verificationScope = factory.Services.CreateAsyncScope();
        AppDbContext db = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Domain.Entities.ChatMessage assistantMessage = await db.ChatMessages.SingleAsync(message =>
            message.ConversationId == conversation.Id &&
            message.Role == ChatRole.Assistant);
        Assert.Equal(answer.Answer, assistantMessage.Content);
    }

    [Fact]
    public async Task SendChatMessage_Should_Return_NotFound_For_Missing_Conversation()
    {
        using LocalApiTestFactory factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/chats/{Guid.NewGuid()}/messages",
            new ChatMessageRequest("Hello"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SendChatMessage_Should_Return_ValidationProblemDetails_For_Empty_Message()
    {
        using LocalApiTestFactory factory = new();
        using HttpClient client = factory.CreateClient();
        ConversationDto conversation = await ApiScenarioHelpers.CreateConversationAsync(client);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/chats/{conversation.Id}/messages",
            new ChatMessageRequest("   "));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Microsoft.AspNetCore.Mvc.ValidationProblemDetails? problem =
            await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("chats.validationFailed", problem.Extensions["code"]?.ToString());
        Assert.Contains("content", problem.Errors.Keys);
    }

    [Fact]
    public async Task SendChatMessage_Should_Not_Return_Sources_From_Deleted_Documents()
    {
        using LocalApiTestFactory factory = new();
        using HttpClient client = factory.CreateClient();
        string documentText = $"Deleted document source {Guid.NewGuid():N}";
        UploadDocumentResponse upload = await ApiScenarioHelpers.UploadTextDocumentAsync(client, documentText, $"deleted-rag-{Guid.NewGuid():N}.txt");

        await using (AsyncServiceScope setupScope = factory.Services.CreateAsyncScope())
        {
            IIngestionJobProcessor processor = setupScope.ServiceProvider.GetRequiredService<IIngestionJobProcessor>();
            await processor.ProcessAsync(upload.IngestionJobId);
            AppDbContext setupDb = setupScope.ServiceProvider.GetRequiredService<AppDbContext>();
            Domain.Entities.Document document = await setupDb.Documents.SingleAsync(item => item.Id == upload.DocumentId);
            document.DeletedAt = DateTimeOffset.UtcNow;
            await setupDb.SaveChangesAsync();
        }

        ConversationDto conversation = await ApiScenarioHelpers.CreateConversationAsync(client);
        HttpResponseMessage sendResponse = await client.PostAsJsonAsync(
            $"/api/chats/{conversation.Id}/messages",
            new ChatMessageRequest(documentText));

        Assert.Equal(HttpStatusCode.OK, sendResponse.StatusCode);
        RagAnswerDto? answer = await sendResponse.Content.ReadFromJsonAsync<RagAnswerDto>();
        Assert.NotNull(answer);
        Assert.Empty(answer.Sources);
        Assert.DoesNotContain(documentText, answer.Answer, StringComparison.Ordinal);
    }

}
