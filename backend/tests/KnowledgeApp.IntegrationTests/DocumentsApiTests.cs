using System.Net;
using System.Net.Http.Json;
using KnowledgeApp.Contracts.Documents;
using Microsoft.AspNetCore.Mvc.Testing;

namespace KnowledgeApp.IntegrationTests;

public sealed class DocumentsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public DocumentsApiTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task UploadDocument_Should_Create_Document_And_Return_It_From_Query_Endpoints()
    {
        using var client = factory.CreateClient();
        var fileName = $"integration-{Guid.NewGuid():N}.txt";
        using var form = new MultipartFormDataContent();
        using var file = new ByteArrayContent("hello from integration test"u8.ToArray());
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
        form.Add(file, "file", fileName);

        using var uploadResponse = await client.PostAsync("/api/documents/upload", form);

        Assert.Equal(HttpStatusCode.Created, uploadResponse.StatusCode);
        var upload = await uploadResponse.Content.ReadFromJsonAsync<UploadDocumentResponse>();
        Assert.NotNull(upload);

        var documents = await client.GetFromJsonAsync<DocumentDto[]>("/api/documents");
        Assert.Contains(documents ?? [], document => document.Id == upload.DocumentId && document.Name == fileName);

        var document = await client.GetFromJsonAsync<DocumentDto>($"/api/documents/{upload.DocumentId}");
        Assert.NotNull(document);
        Assert.Equal(fileName, document.Name);
    }

    [Fact]
    public async Task GetDocumentById_Should_Return_NotFound_When_Document_Is_Missing()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/api/documents/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
