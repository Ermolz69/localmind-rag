using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using KnowledgeApp.Contracts.Documents;
using KnowledgeApp.Contracts.Search;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeApp.IntegrationTests;

public sealed class ContentSearchApiTests : IClassFixture<LocalApiTestFactory>
{
    private readonly LocalApiTestFactory factory;

    public ContentSearchApiTests(LocalApiTestFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task ContentSearch_Should_Return_Matching_Note_And_Processed_Uploaded_Document()
    {
        using HttpClient client = factory.CreateClient();
        string marker = $"contentboth{Guid.NewGuid():N}";

        Guid noteId = await AddNoteAsync(
            $"Search validation note {marker}",
            $"This note contains {marker} and must appear in backend content search.");

        UploadDocumentResponse upload = await UploadAndProcessTextDocumentAsync(client, marker);

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/search/content",
            new ContentSearchRequest(
                marker,
                Limit: 10,
                IncludeDocuments: true,
                IncludeNotes: true));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ContentSearchResponse? body =
            await response.Content.ReadFromJsonAsync<ContentSearchResponse>();

        Assert.NotNull(body);

        ContentSearchHitDto noteResult = Assert.Single(
            body.Results,
            result =>
                result.SourceType == "note" &&
                result.SourceId == noteId);

        Assert.Equal("Search validation note " + marker, noteResult.Title);
        Assert.Null(noteResult.ChunkId);
        Assert.Contains(marker, noteResult.Snippet, StringComparison.OrdinalIgnoreCase);

        ContentSearchHitDto documentResult = Assert.Single(
            body.Results,
            result =>
                result.SourceType == "document" &&
                result.SourceId == upload.DocumentId);

        Assert.Equal($"content-search-{marker}.txt", documentResult.Title);
        Assert.NotNull(documentResult.ChunkId);
        Assert.Contains(marker, documentResult.Snippet, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ContentSearch_Should_Return_Only_Notes_When_Document_Search_Is_Disabled()
    {
        using HttpClient client = factory.CreateClient();
        string marker = $"notesonly{Guid.NewGuid():N}";

        Guid noteId = await AddNoteAsync(
            $"Note {marker}",
            $"Note markdown includes {marker}.");

        await AddIndexedDocumentChunkAsync(
            $"Document {marker}.txt",
            $"Document text includes {marker}.");

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/search/content",
            new ContentSearchRequest(
                marker,
                Limit: 10,
                IncludeDocuments: false,
                IncludeNotes: true));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ContentSearchResponse? body =
            await response.Content.ReadFromJsonAsync<ContentSearchResponse>();

        Assert.NotNull(body);

        ContentSearchHitDto result = Assert.Single(body.Results);

        Assert.Equal("note", result.SourceType);
        Assert.Equal(noteId, result.SourceId);
        Assert.Null(result.ChunkId);
    }

    [Fact]
    public async Task ContentSearch_Should_Return_Only_Documents_When_Note_Search_Is_Disabled()
    {
        using HttpClient client = factory.CreateClient();
        string marker = $"documentsonly{Guid.NewGuid():N}";

        await AddNoteAsync(
            $"Note {marker}",
            $"Note markdown includes {marker}.");

        (Guid documentId, Guid chunkId) = await AddIndexedDocumentChunkAsync(
            $"Document {marker}.txt",
            $"Document text includes {marker}.");

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/search/content",
            new ContentSearchRequest(
                marker,
                Limit: 10,
                IncludeDocuments: true,
                IncludeNotes: false));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ContentSearchResponse? body =
            await response.Content.ReadFromJsonAsync<ContentSearchResponse>();

        Assert.NotNull(body);

        ContentSearchHitDto result = Assert.Single(body.Results);

        Assert.Equal("document", result.SourceType);
        Assert.Equal(documentId, result.SourceId);
        Assert.Equal(chunkId, result.ChunkId);
    }

    [Fact]
    public async Task ContentSearch_Should_Return_Empty_Results_When_Text_Does_Not_Match()
    {
        using HttpClient client = factory.CreateClient();
        string marker = $"nothingmatches{Guid.NewGuid():N}";

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/search/content",
            new ContentSearchRequest(
                marker,
                Limit: 10,
                IncludeDocuments: true,
                IncludeNotes: true));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ContentSearchResponse? body =
            await response.Content.ReadFromJsonAsync<ContentSearchResponse>();

        Assert.NotNull(body);
        Assert.Empty(body.Results);
    }

    [Fact]
    public async Task ContentSearch_Should_Not_Return_Deleted_Notes_Or_Unindexed_Documents()
    {
        using HttpClient client = factory.CreateClient();
        string marker = $"hiddencontent{Guid.NewGuid():N}";

        await AddNoteAsync(
            $"Deleted note {marker}",
            $"Deleted note contains {marker}.",
            deleted: true);

        await AddDocumentChunkAsync(
            $"Uploaded document {marker}.txt",
            $"Uploaded but not indexed document contains {marker}.",
            DocumentStatus.Uploaded);

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/search/content",
            new ContentSearchRequest(
                marker,
                Limit: 10,
                IncludeDocuments: true,
                IncludeNotes: true));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ContentSearchResponse? body =
            await response.Content.ReadFromJsonAsync<ContentSearchResponse>();

        Assert.NotNull(body);
        Assert.Empty(body.Results);
    }

    [Fact]
    public async Task ContentSearch_Should_Respect_Limit()
    {
        using HttpClient client = factory.CreateClient();
        string marker = $"limitedcontent{Guid.NewGuid():N}";

        await AddNoteAsync(
            $"First note {marker}",
            $"First note contains {marker}.");

        await AddNoteAsync(
            $"Second note {marker}",
            $"Second note contains {marker}.");

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/search/content",
            new ContentSearchRequest(
                marker,
                Limit: 1,
                IncludeDocuments: false,
                IncludeNotes: true));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ContentSearchResponse? body =
            await response.Content.ReadFromJsonAsync<ContentSearchResponse>();

        Assert.NotNull(body);
        Assert.Single(body.Results);
        Assert.Equal("note", body.Results[0].SourceType);
    }

    [Fact]
    public async Task ContentSearch_Should_Return_BadRequest_When_Query_Is_Empty()
    {
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/search/content",
            new ContentSearchRequest(
                string.Empty,
                Limit: 10,
                IncludeDocuments: true,
                IncludeNotes: true));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<Guid> AddNoteAsync(
        string title,
        string markdown,
        bool deleted = false)
    {
        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Note note = new()
        {
            Title = title,
            Markdown = markdown,
            DeletedAt = deleted ? DateTimeOffset.UtcNow : null,
        };

        db.Notes.Add(note);
        await db.SaveChangesAsync();

        return note.Id;
    }

    private async Task<(Guid DocumentId, Guid ChunkId)> AddIndexedDocumentChunkAsync(
        string documentName,
        string chunkText)
    {
        return await AddDocumentChunkAsync(
            documentName,
            chunkText,
            DocumentStatus.Indexed);
    }

    private async Task<(Guid DocumentId, Guid ChunkId)> AddDocumentChunkAsync(
        string documentName,
        string chunkText,
        DocumentStatus status)
    {
        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Document document = new()
        {
            Name = documentName,
            Status = status,
        };

        DocumentChunk chunk = new()
        {
            DocumentId = document.Id,
            Index = 0,
            PageNumber = 1,
            Text = chunkText,
        };

        db.Documents.Add(document);
        db.DocumentChunks.Add(chunk);
        await db.SaveChangesAsync();

        return (document.Id, chunk.Id);
    }

    private static async Task<UploadDocumentResponse> UploadAndProcessTextDocumentAsync(
        HttpClient client,
        string marker)
    {
        string fileName = $"content-search-{marker}.txt";
        string content =
            $"This uploaded document contains {marker} and must appear in backend content search.";

        using MultipartFormDataContent form = new();
        using ByteArrayContent file =
            new(Encoding.UTF8.GetBytes(content));

        file.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        form.Add(file, "file", fileName);

        using HttpResponseMessage uploadResponse =
            await client.PostAsync("/api/documents/upload", form);

        Assert.Equal(HttpStatusCode.Created, uploadResponse.StatusCode);

        UploadDocumentResponse? upload =
            await uploadResponse.Content.ReadFromJsonAsync<UploadDocumentResponse>();

        Assert.NotNull(upload);

        using HttpResponseMessage processResponse =
            await client.PostAsync(
                $"/api/ingestion/jobs/{upload.IngestionJobId}/process",
                content: null);

        Assert.Equal(HttpStatusCode.Accepted, processResponse.StatusCode);

        return upload;
    }
}
