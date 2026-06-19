using System.Net;
using System.Net.Http.Json;
using System.Text;

using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Chats;
using KnowledgeApp.Contracts.Documents;
using KnowledgeApp.Contracts.Rag;
using KnowledgeApp.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeApp.IntegrationTests.TestSupport;

internal static class ApiScenarioHelpers
{
    public static async Task<ConversationDto> CreateConversationAsync(
        HttpClient client,
        string? title = null)
    {
        using HttpResponseMessage createResponse = await client.PostAsJsonAsync(
            "/api/v1/chats",
            new CreateConversationRequest(title ?? $"Test chat {Guid.NewGuid():N}"));

        createResponse.EnsureSuccessStatusCode();

        ConversationDto? conversation =
            await createResponse.Content.ReadApiDataAsync<ConversationDto>();

        Assert.NotNull(conversation);

        return conversation;
    }

    public static async Task<UploadDocumentResponse> UploadTextDocumentAsync(
        HttpClient client,
        string content,
        string? fileName = null)
    {
        using MultipartFormDataContent form = new();
        using ByteArrayContent file = new(Encoding.UTF8.GetBytes(content));

        file.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");

        form.Add(file, "file", fileName ?? $"document-{Guid.NewGuid():N}.txt");

        using HttpResponseMessage uploadResponse =
            await client.PostAsync("/api/v1/documents/upload", form);

        Assert.Equal(HttpStatusCode.Created, uploadResponse.StatusCode);

        UploadDocumentResponse? upload =
            await uploadResponse.Content.ReadApiDataAsync<UploadDocumentResponse>();

        Assert.NotNull(upload);

        return upload;
    }

    public static async Task<UploadDocumentResponse> UploadAndIngestAsync(
        HttpClient client,
        IServiceProvider services,
        string content,
        string? fileName = null)
    {
        UploadDocumentResponse upload = await UploadTextDocumentAsync(client, content, fileName);

        await using AsyncServiceScope scope = services.CreateAsyncScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        KnowledgeApp.Domain.Entities.IngestionJob job =
            await db.IngestionJobs.FirstAsync(j => j.DocumentId == upload.DocumentId);
        IIngestionJobProcessor processor =
            scope.ServiceProvider.GetRequiredService<IIngestionJobProcessor>();
        await processor.ProcessAsync(job.Id);

        return upload;
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
