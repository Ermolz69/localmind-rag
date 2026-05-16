using System.Net;
using System.Net.Http.Json;
using KnowledgeApp.Contracts.Buckets;
using KnowledgeApp.Contracts.Chats;
using KnowledgeApp.Contracts.Common;
using KnowledgeApp.Contracts.Documents;
using KnowledgeApp.Contracts.Notes;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeApp.IntegrationTests;

public sealed class LocalApiStructureSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public LocalApiStructureSmokeTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Bucket_Note_And_Chat_Endpoints_Should_Keep_Current_Wire_Shape()
    {
        using HttpClient? client = factory.CreateClient();

        HttpResponseMessage? bucketResponse = await client.PostAsJsonAsync(
            "/api/buckets",
            new CreateBucketRequest($"Bucket-{Guid.NewGuid():N}", Description: null));
        HttpResponseMessage? noteResponse = await client.PostAsJsonAsync(
            "/api/notes",
            new CreateNoteRequest(BucketId: null, "Note", "Body"));
        HttpResponseMessage? chatResponse = await client.PostAsJsonAsync("/api/chats", new CreateConversationRequest("Chat"));

        Assert.Equal(HttpStatusCode.Created, bucketResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, noteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, chatResponse.StatusCode);
        Assert.NotNull(await client.GetFromJsonAsync<BucketDto[]>("/api/buckets"));
        Assert.NotNull(await client.GetFromJsonAsync<CursorPage<NoteDto>>("/api/notes"));
        Assert.NotNull(await client.GetFromJsonAsync<CursorPage<ConversationDto>>("/api/chats"));
    }

    [Fact]
    public async Task Chat_Endpoints_Should_Get_Update_And_List_Messages()
    {
        using HttpClient client = factory.CreateClient();
        HttpResponseMessage createResponse = await client.PostAsJsonAsync(
            "/api/chats",
            new CreateConversationRequest("Initial chat"));
        ConversationDto? created = await createResponse.Content.ReadFromJsonAsync<ConversationDto>();
        Assert.NotNull(created);

        ConversationDto? fetched = await client.GetFromJsonAsync<ConversationDto>($"/api/chats/{created.Id}");
        Assert.NotNull(fetched);
        Assert.Equal("Initial chat", fetched.Title);

        HttpResponseMessage updateResponse = await client.PutAsJsonAsync(
            $"/api/chats/{created.Id}",
            new UpdateConversationRequest("Renamed chat"));
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        HttpResponseMessage sendResponse = await client.PostAsJsonAsync(
            $"/api/chats/{created.Id}/messages",
            new KnowledgeApp.Contracts.Rag.ChatMessageRequest("Hello"));
        Assert.Equal(HttpStatusCode.OK, sendResponse.StatusCode);

        ChatMessageDto[]? messages = await client.GetFromJsonAsync<ChatMessageDto[]>($"/api/chats/{created.Id}/messages");
        Assert.NotNull(messages);
        Assert.Contains(messages, message => message.Content == "Hello");
    }

    [Fact]
    public async Task DeleteChat_Should_Soft_Delete_Conversation_And_Hide_Messages()
    {
        using HttpClient client = factory.CreateClient();
        HttpResponseMessage createResponse = await client.PostAsJsonAsync(
            "/api/chats",
            new CreateConversationRequest("Delete me"));
        ConversationDto? created = await createResponse.Content.ReadFromJsonAsync<ConversationDto>();
        Assert.NotNull(created);
        HttpResponseMessage sendResponse = await client.PostAsJsonAsync(
            $"/api/chats/{created.Id}/messages",
            new KnowledgeApp.Contracts.Rag.ChatMessageRequest("Before delete"));
        Assert.Equal(HttpStatusCode.OK, sendResponse.StatusCode);

        HttpResponseMessage deleteResponse = await client.DeleteAsync($"/api/chats/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        using HttpResponseMessage getResponse = await client.GetAsync($"/api/chats/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);

        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Conversation storedConversation = await db.Conversations.SingleAsync(item => item.Id == created.Id);
        ChatMessage[] storedMessages = await db.ChatMessages
            .Where(message => message.ConversationId == created.Id)
            .ToArrayAsync();

        Assert.NotNull(storedConversation.DeletedAt);
        Assert.All(storedMessages, message => Assert.NotNull(message.DeletedAt));
    }

    [Fact]
    public async Task ReindexDocumentEndpoint_Should_Create_Queued_Ingestion_Job()
    {
        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        AppDbContext? db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Document? document = new Document { Name = $"reindex-{Guid.NewGuid():N}.txt", Status = DocumentStatus.Indexed };
        db.Documents.Add(document);
        await db.SaveChangesAsync();

        using HttpClient? client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync($"/api/documents/{document.Id}/reindex", content: null);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        ReindexDocumentResponse? body = await response.Content.ReadFromJsonAsync<ReindexDocumentResponse>();
        Assert.NotNull(body);
        Assert.Equal(document.Id, body.DocumentId);
        Assert.True(await db.IngestionJobs.AnyAsync(job => job.DocumentId == document.Id));
    }
}
