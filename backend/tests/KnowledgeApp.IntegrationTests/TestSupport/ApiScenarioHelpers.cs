using System.Net;
using System.Net.Http.Json;
using System.Text;
using KnowledgeApp.Contracts.Chats;
using KnowledgeApp.Contracts.Documents;

namespace KnowledgeApp.IntegrationTests.TestSupport;

internal static class ApiScenarioHelpers
{
    public static async Task<ConversationDto> CreateConversationAsync(HttpClient client, string? title = null)
    {
        HttpResponseMessage createResponse = await client.PostAsJsonAsync(
            "/api/chats",
            new CreateConversationRequest(title ?? $"Test chat {Guid.NewGuid():N}"));
        createResponse.EnsureSuccessStatusCode();

        ConversationDto? conversation = await createResponse.Content.ReadFromJsonAsync<ConversationDto>();
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
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
        form.Add(file, "file", fileName ?? $"document-{Guid.NewGuid():N}.txt");

        using HttpResponseMessage uploadResponse = await client.PostAsync("/api/documents/upload", form);
        Assert.Equal(HttpStatusCode.Created, uploadResponse.StatusCode);

        UploadDocumentResponse? upload = await uploadResponse.Content.ReadFromJsonAsync<UploadDocumentResponse>();
        Assert.NotNull(upload);
        return upload;
    }
}
