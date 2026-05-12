using System.Net;
using System.Net.Http.Json;
using KnowledgeApp.Application.Buckets;
using KnowledgeApp.Contracts.Documents;
using KnowledgeApp.Domain.Entities;
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

    private static async Task<UploadDocumentResponse> UploadDocumentAsync(HttpClient client, string fileName, Guid? bucketId = null)
    {
        using var form = new MultipartFormDataContent();
        using var file = new ByteArrayContent("hello from integration test"u8.ToArray());
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
        form.Add(file, "file", fileName);

        var url = bucketId.HasValue ? $"/api/documents/upload?bucketId={bucketId}" : "/api/documents/upload";
        using var uploadResponse = await client.PostAsync(url, form);
        Assert.Equal(HttpStatusCode.Created, uploadResponse.StatusCode);
        var upload = await uploadResponse.Content.ReadFromJsonAsync<UploadDocumentResponse>();
        Assert.NotNull(upload);
        return upload;
    }
}
