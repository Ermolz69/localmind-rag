using System.Net;
using System.Net.Http.Json;
using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Buckets;
using KnowledgeApp.Contracts.Documents;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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
        await ClearLastSelectedBucketAsync();
        var fileName = $"integration-{Guid.NewGuid():N}.txt";

        var upload = await UploadDocumentAsync(client, fileName);

        var documents = await client.GetFromJsonAsync<DocumentDto[]>("/api/documents");
        Assert.Contains(documents ?? [], document => document.Id == upload.DocumentId && document.Name == fileName);

        var document = await client.GetFromJsonAsync<DocumentDto>($"/api/documents/{upload.DocumentId}");
        Assert.NotNull(document);
        Assert.Equal(fileName, document.Name);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var storedDocument = await db.Documents.SingleAsync(x => x.Id == upload.DocumentId);
        var defaultBucket = await db.Buckets.SingleAsync(x => x.Id == storedDocument.BucketId);
        var storedFile = await db.DocumentFiles.SingleAsync(x => x.DocumentId == upload.DocumentId);
        var ingestionJob = await db.IngestionJobs.SingleAsync(x => x.DocumentId == upload.DocumentId);

        Assert.Equal(BucketConstants.DefaultBucketName, defaultBucket.Name);
        Assert.Equal(fileName, storedFile.FileName);
        Assert.Equal(upload.IngestionJobId, ingestionJob.Id);
    }

    [Fact]
    public async Task UploadDocument_Should_Use_Selected_Bucket_And_Filter_By_Bucket()
    {
        using var client = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var selectedBucket = new Bucket { Name = $"Selected-{Guid.NewGuid():N}" };
        var otherBucket = new Bucket { Name = $"Other-{Guid.NewGuid():N}" };
        db.Buckets.AddRange(selectedBucket, otherBucket);
        await db.SaveChangesAsync();

        var selectedFileName = $"selected-{Guid.NewGuid():N}.txt";
        var otherFileName = $"other-{Guid.NewGuid():N}.txt";
        var selectedUpload = await UploadDocumentAsync(client, selectedFileName, selectedBucket.Id);
        var otherUpload = await UploadDocumentAsync(client, otherFileName, otherBucket.Id);

        var filteredDocuments = await client.GetFromJsonAsync<DocumentDto[]>($"/api/documents?bucketId={selectedBucket.Id}");
        Assert.Contains(filteredDocuments ?? [], document => document.Id == selectedUpload.DocumentId);
        Assert.DoesNotContain(filteredDocuments ?? [], document => document.Id == otherUpload.DocumentId);

        await db.Entry(selectedBucket).ReloadAsync();
        await db.Entry(otherBucket).ReloadAsync();
        var selectedDocument = await db.Documents.SingleAsync(x => x.Id == selectedUpload.DocumentId);
        Assert.Equal(selectedBucket.Id, selectedDocument.BucketId);
    }

    [Fact]
    public async Task GetDocumentById_Should_Return_NotFound_When_Document_Is_Missing()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/api/documents/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Uploaded_Text_Document_Should_Be_Processed_Into_Chunks()
    {
        using var client = factory.CreateClient();
        var fileName = $"ingestion-{Guid.NewGuid():N}.txt";
        var upload = await UploadDocumentAsync(client, fileName, content: "First paragraph.\n\nSecond paragraph.");

        await using var scope = factory.Services.CreateAsyncScope();
        var processor = scope.ServiceProvider.GetRequiredService<IIngestionJobProcessor>();
        await processor.ProcessAsync(upload.IngestionJobId);

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var chunks = await db.DocumentChunks
            .Where(x => x.DocumentId == upload.DocumentId)
            .OrderBy(x => x.Index)
            .ToArrayAsync();
        var document = await db.Documents.SingleAsync(x => x.Id == upload.DocumentId);
        var job = await db.IngestionJobs.SingleAsync(x => x.Id == upload.IngestionJobId);

        Assert.Single(chunks);
        Assert.Equal("First paragraph.\n\nSecond paragraph.", chunks[0].Text);
        Assert.Equal(DocumentStatus.Indexed, document.Status);
        Assert.Equal(IngestionJobStatus.Completed, job.Status);
    }

    [Fact]
    public async Task ProcessIngestionJobEndpoint_Should_Process_Uploaded_Text_Document()
    {
        using var client = factory.CreateClient();
        var fileName = $"endpoint-ingestion-{Guid.NewGuid():N}.txt";
        var upload = await UploadDocumentAsync(client, fileName, content: "Endpoint paragraph.");

        using var response = await client.PostAsync($"/api/ingestion/jobs/{upload.IngestionJobId}/process", content: null);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var chunk = await db.DocumentChunks.SingleAsync(x => x.DocumentId == upload.DocumentId);
        var document = await db.Documents.SingleAsync(x => x.Id == upload.DocumentId);
        Assert.Equal("Endpoint paragraph.", chunk.Text);
        Assert.Equal(DocumentStatus.Indexed, document.Status);
    }

    [Fact]
    public async Task Uploaded_Pdf_Document_Should_Fail_Ingestion_For_Mvp()
    {
        using var client = factory.CreateClient();
        var fileName = $"unsupported-{Guid.NewGuid():N}.pdf";
        var upload = await UploadDocumentAsync(client, fileName, content: "%PDF skeleton", contentType: "application/pdf");

        await using var scope = factory.Services.CreateAsyncScope();
        var processor = scope.ServiceProvider.GetRequiredService<IIngestionJobProcessor>();
        await processor.ProcessAsync(upload.IngestionJobId);

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var document = await db.Documents.SingleAsync(x => x.Id == upload.DocumentId);
        var job = await db.IngestionJobs.SingleAsync(x => x.Id == upload.IngestionJobId);

        Assert.Equal(DocumentStatus.Failed, document.Status);
        Assert.Equal(IngestionJobStatus.Failed, job.Status);
        Assert.Contains("not supported", job.LastError, StringComparison.OrdinalIgnoreCase);
    }

    private async Task ClearLastSelectedBucketAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var settings = await db.AppSettings
            .Where(x => x.Key == BucketSettingsKeys.LastSelectedBucketId)
            .ToArrayAsync();
        db.AppSettings.RemoveRange(settings);
        await db.SaveChangesAsync();
    }

    private static async Task<UploadDocumentResponse> UploadDocumentAsync(
        HttpClient client,
        string fileName,
        Guid? bucketId = null,
        string content = "hello from integration test",
        string contentType = "text/plain")
    {
        using var form = new MultipartFormDataContent();
        using var file = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(content));
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        form.Add(file, "file", fileName);

        var url = bucketId.HasValue ? $"/api/documents/upload?bucketId={bucketId}" : "/api/documents/upload";
        using var uploadResponse = await client.PostAsync(url, form);
        Assert.Equal(HttpStatusCode.Created, uploadResponse.StatusCode);
        var upload = await uploadResponse.Content.ReadFromJsonAsync<UploadDocumentResponse>();
        Assert.NotNull(upload);
        return upload;
    }
}
