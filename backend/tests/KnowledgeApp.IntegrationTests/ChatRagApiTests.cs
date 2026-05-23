using System.Net;
using System.Net.Http.Json;
using System.Text;
using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Chats;
using KnowledgeApp.Contracts.Documents;
using KnowledgeApp.Contracts.Rag;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeApp.IntegrationTests;

public sealed class ChatRagApiTests : IClassFixture<LocalApiTestFactory>
{
    private readonly LocalApiTestFactory factory;

    public ChatRagApiTests(LocalApiTestFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task SendChatMessage_Should_Search_Chunks_Save_Messages_And_Return_Sources()
    {
        using HttpClient client = factory.CreateClient();
        string documentText = $"Localmind RAG test source {Guid.NewGuid():N}";
        UploadDocumentResponse upload = await UploadDocumentAsync(client, $"rag-chat-{Guid.NewGuid():N}.txt", documentText);

        await using (AsyncServiceScope ingestionScope = factory.Services.CreateAsyncScope())
        {
            IIngestionJobProcessor processor = ingestionScope.ServiceProvider.GetRequiredService<IIngestionJobProcessor>();
            await processor.ProcessAsync(upload.IngestionJobId);
        }

        HttpResponseMessage createResponse = await client.PostAsJsonAsync(
            "/api/chats",
            new CreateConversationRequest("RAG chat"));
        createResponse.EnsureSuccessStatusCode();
        ConversationDto? conversation = await createResponse.Content.ReadFromJsonAsync<ConversationDto>();
        Assert.NotNull(conversation);

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

    private static async Task<UploadDocumentResponse> UploadDocumentAsync(
        HttpClient client,
        string fileName,
        string content)
    {
        using MultipartFormDataContent form = new();
        using ByteArrayContent file = new(Encoding.UTF8.GetBytes(content));
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
        form.Add(file, "file", fileName);

        using HttpResponseMessage uploadResponse = await client.PostAsync("/api/documents/upload", form);
        Assert.Equal(HttpStatusCode.Created, uploadResponse.StatusCode);
        UploadDocumentResponse? upload = await uploadResponse.Content.ReadFromJsonAsync<UploadDocumentResponse>();
        Assert.NotNull(upload);
        return upload;
    }
}
