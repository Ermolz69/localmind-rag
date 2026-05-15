using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Buckets;
using KnowledgeApp.Contracts.Common;
using KnowledgeApp.Contracts.Documents;
using KnowledgeApp.Contracts.Ingestion;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using A = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;
using W = DocumentFormat.OpenXml.Wordprocessing;

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
        using HttpClient? client = factory.CreateClient();
        await ClearLastSelectedBucketAsync();
        string? fileName = $"integration-{Guid.NewGuid():N}.txt";

        UploadDocumentResponse? upload = await UploadDocumentAsync(client, fileName);

        CursorPage<DocumentDto>? documents = await client.GetFromJsonAsync<CursorPage<DocumentDto>>("/api/documents");
        Assert.Contains(documents?.Items ?? [], document => document.Id == upload.DocumentId && document.Name == fileName);

        DocumentDto? document = await client.GetFromJsonAsync<DocumentDto>($"/api/documents/{upload.DocumentId}");
        Assert.NotNull(document);
        Assert.Equal(fileName, document.Name);

        using IServiceScope? scope = factory.Services.CreateScope();
        AppDbContext? db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Document? storedDocument = await db.Documents.SingleAsync(x => x.Id == upload.DocumentId);
        Bucket? defaultBucket = await db.Buckets.SingleAsync(x => x.Id == storedDocument.BucketId);
        DocumentFile? storedFile = await db.DocumentFiles.SingleAsync(x => x.DocumentId == upload.DocumentId);
        IngestionJob? ingestionJob = await db.IngestionJobs.SingleAsync(x => x.DocumentId == upload.DocumentId);

        Assert.Equal(BucketConstants.DefaultBucketName, defaultBucket.Name);
        Assert.Equal(fileName, storedFile.FileName);
        Assert.Equal(upload.IngestionJobId, ingestionJob.Id);
    }

    [Fact]
    public async Task UploadDocument_Should_Use_Selected_Bucket_And_Filter_By_Bucket()
    {
        using HttpClient? client = factory.CreateClient();
        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        AppDbContext? db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Bucket? selectedBucket = new Bucket { Name = $"Selected-{Guid.NewGuid():N}" };
        Bucket? otherBucket = new Bucket { Name = $"Other-{Guid.NewGuid():N}" };
        db.Buckets.AddRange(selectedBucket, otherBucket);
        await db.SaveChangesAsync();

        string? selectedFileName = $"selected-{Guid.NewGuid():N}.txt";
        string? otherFileName = $"other-{Guid.NewGuid():N}.txt";
        UploadDocumentResponse? selectedUpload = await UploadDocumentAsync(client, selectedFileName, selectedBucket.Id);
        UploadDocumentResponse? otherUpload = await UploadDocumentAsync(client, otherFileName, otherBucket.Id);

        CursorPage<DocumentDto>? filteredDocuments = await client.GetFromJsonAsync<CursorPage<DocumentDto>>($"/api/documents?bucketId={selectedBucket.Id}");
        Assert.Contains(filteredDocuments?.Items ?? [], document => document.Id == selectedUpload.DocumentId);
        Assert.DoesNotContain(filteredDocuments?.Items ?? [], document => document.Id == otherUpload.DocumentId);

        await db.Entry(selectedBucket).ReloadAsync();
        await db.Entry(otherBucket).ReloadAsync();
        Document? selectedDocument = await db.Documents.SingleAsync(x => x.Id == selectedUpload.DocumentId);
        Assert.Equal(selectedBucket.Id, selectedDocument.BucketId);
    }

    [Fact]
    public async Task GetDocuments_Should_Return_Cursor_Page_And_Use_Next_Cursor()
    {
        using HttpClient client = factory.CreateClient();
        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Bucket bucket = new() { Name = $"Cursor-{Guid.NewGuid():N}" };
        db.Buckets.Add(bucket);
        await db.SaveChangesAsync();
        UploadDocumentResponse firstUpload = await UploadDocumentAsync(client, $"cursor-a-{Guid.NewGuid():N}.txt", bucket.Id);
        UploadDocumentResponse secondUpload = await UploadDocumentAsync(client, $"cursor-b-{Guid.NewGuid():N}.txt", bucket.Id);

        CursorPage<DocumentDto>? firstPage = await client.GetFromJsonAsync<CursorPage<DocumentDto>>(
            $"/api/documents?bucketId={bucket.Id}&limit=1");
        Assert.NotNull(firstPage);
        Assert.True(firstPage.HasMore);
        Assert.NotNull(firstPage.NextCursor);

        CursorPage<DocumentDto>? secondPage = await client.GetFromJsonAsync<CursorPage<DocumentDto>>(
            $"/api/documents?bucketId={bucket.Id}&limit=1&cursor={Uri.EscapeDataString(firstPage.NextCursor)}");
        Assert.NotNull(secondPage);
        Assert.DoesNotContain(secondPage.Items, document => document.Id == firstPage.Items[0].Id);
        Guid[] returnedDocumentIds = firstPage.Items.Concat(secondPage.Items).Select(document => document.Id).ToArray();
        Assert.Contains(firstUpload.DocumentId, returnedDocumentIds);
        Assert.Contains(secondUpload.DocumentId, returnedDocumentIds);
    }

    [Fact]
    public async Task GetDocumentById_Should_Return_NotFound_When_Document_Is_Missing()
    {
        using HttpClient? client = factory.CreateClient();

        using HttpResponseMessage? response = await client.GetAsync($"/api/documents/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UploadDocument_Should_Return_ValidationProblemDetails_For_Unsupported_File()
    {
        using HttpClient? client = factory.CreateClient();
        using MultipartFormDataContent? form = new MultipartFormDataContent();
        using ByteArrayContent? file = new ByteArrayContent("unsupported"u8.ToArray());
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        form.Add(file, "file", "unsupported.exe");

        using HttpResponseMessage? response = await client.PostAsync("/api/documents/upload", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("documents.unsupportedFileType", problem.Extensions["code"]?.ToString());
        Assert.Contains("fileName", problem.Errors.Keys);
    }

    [Fact]
    public async Task UploadDocument_Should_Return_ProblemDetails_For_Missing_Selected_Bucket()
    {
        using HttpClient? client = factory.CreateClient();
        using MultipartFormDataContent? form = new MultipartFormDataContent();
        using ByteArrayContent? file = new ByteArrayContent("content"u8.ToArray());
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
        form.Add(file, "file", "notes.txt");

        using HttpResponseMessage? response = await client.PostAsync($"/api/documents/upload?bucketId={Guid.NewGuid()}", form);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("buckets.notFound", problem.Extensions["code"]?.ToString());
        Assert.False(string.IsNullOrWhiteSpace(problem.Extensions["traceId"]?.ToString()));
    }

    [Fact]
    public async Task Uploaded_Text_Document_Should_Be_Processed_Into_Chunks()
    {
        using HttpClient? client = factory.CreateClient();
        string? fileName = $"ingestion-{Guid.NewGuid():N}.txt";
        UploadDocumentResponse? upload = await UploadDocumentAsync(client, fileName, content: "First paragraph.\n\nSecond paragraph.");

        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        IIngestionJobProcessor? processor = scope.ServiceProvider.GetRequiredService<IIngestionJobProcessor>();
        await processor.ProcessAsync(upload.IngestionJobId);

        AppDbContext? db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        DocumentChunk[]? chunks = await db.DocumentChunks
            .Where(x => x.DocumentId == upload.DocumentId)
            .OrderBy(x => x.Index)
            .ToArrayAsync();
        Document? document = await db.Documents.SingleAsync(x => x.Id == upload.DocumentId);
        IngestionJob? job = await db.IngestionJobs.SingleAsync(x => x.Id == upload.IngestionJobId);

        Assert.Single(chunks);
        Assert.Equal("First paragraph.\n\nSecond paragraph.", chunks[0].Text);
        Assert.Equal(DocumentStatus.Indexed, document.Status);
        Assert.Equal(IngestionJobStatus.Completed, job.Status);

        DocumentEmbedding? embedding = await db.DocumentEmbeddings.SingleAsync(x => x.DocumentChunkId == chunks[0].Id);
        Assert.Equal("BGE-M3", embedding.ModelName);
        Assert.Equal(32, embedding.Dimension);
        Assert.Equal(32 * sizeof(float), embedding.Embedding.Length);
    }

    [Fact]
    public async Task ProcessIngestionJobEndpoint_Should_Process_Uploaded_Text_Document()
    {
        using HttpClient? client = factory.CreateClient();
        string? fileName = $"endpoint-ingestion-{Guid.NewGuid():N}.txt";
        UploadDocumentResponse? upload = await UploadDocumentAsync(client, fileName, content: "Endpoint paragraph.");

        using HttpResponseMessage? response = await client.PostAsync($"/api/ingestion/jobs/{upload.IngestionJobId}/process", content: null);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        ProcessIngestionJobResponse? body = await response.Content.ReadFromJsonAsync<ProcessIngestionJobResponse>();
        Assert.NotNull(body);
        Assert.Equal(upload.IngestionJobId, body.JobId);
        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        AppDbContext? db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        DocumentChunk? chunk = await db.DocumentChunks.SingleAsync(x => x.DocumentId == upload.DocumentId);
        Document? document = await db.Documents.SingleAsync(x => x.Id == upload.DocumentId);
        Assert.Equal("Endpoint paragraph.", chunk.Text);
        Assert.Equal(DocumentStatus.Indexed, document.Status);
    }

    [Fact]
    public async Task Uploaded_Corrupt_Pdf_Document_Should_Fail_Ingestion()
    {
        using HttpClient? client = factory.CreateClient();
        string? fileName = $"unsupported-{Guid.NewGuid():N}.pdf";
        UploadDocumentResponse? upload = await UploadDocumentAsync(client, fileName, content: "%PDF skeleton", contentType: "application/pdf");

        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        IIngestionJobProcessor? processor = scope.ServiceProvider.GetRequiredService<IIngestionJobProcessor>();
        await processor.ProcessAsync(upload.IngestionJobId);

        AppDbContext? db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Document? document = await db.Documents.SingleAsync(x => x.Id == upload.DocumentId);
        IngestionJob? job = await db.IngestionJobs.SingleAsync(x => x.Id == upload.IngestionJobId);

        Assert.Equal(DocumentStatus.Failed, document.Status);
        Assert.Equal(IngestionJobStatus.Failed, job.Status);
        Assert.Contains("PDF", job.LastError, StringComparison.OrdinalIgnoreCase);

        CursorPage<DocumentDto>? documents = await client.GetFromJsonAsync<CursorPage<DocumentDto>>("/api/documents");
        Assert.Contains(documents?.Items ?? [], item => item.Id == upload.DocumentId && item.LastError != null);
    }

    [Fact]
    public async Task Uploaded_Pdf_Document_Should_Be_Processed_Into_Page_Mapped_Chunks()
    {
        using HttpClient? client = factory.CreateClient();
        string? fileName = $"smoke-{Guid.NewGuid():N}.pdf";
        UploadDocumentResponse? upload = await UploadDocumentAsync(client, fileName, CreatePdfBytes("Integration PDF text."), "application/pdf");

        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        IIngestionJobProcessor? processor = scope.ServiceProvider.GetRequiredService<IIngestionJobProcessor>();
        await processor.ProcessAsync(upload.IngestionJobId);

        AppDbContext? db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        DocumentChunk? chunk = await db.DocumentChunks.SingleAsync(x => x.DocumentId == upload.DocumentId);
        Document? document = await db.Documents.SingleAsync(x => x.Id == upload.DocumentId);

        Assert.Contains("Integration PDF text.", chunk.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, chunk.PageNumber);
        Assert.Equal(DocumentStatus.Indexed, document.Status);
    }

    [Fact]
    public async Task Uploaded_Docx_Document_Should_Be_Processed_Into_Chunks()
    {
        using HttpClient? client = factory.CreateClient();
        string? fileName = $"smoke-{Guid.NewGuid():N}.docx";
        UploadDocumentResponse? upload = await UploadDocumentAsync(
            client,
            fileName,
            CreateDocxBytes("Integration DOCX text."),
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        IIngestionJobProcessor? processor = scope.ServiceProvider.GetRequiredService<IIngestionJobProcessor>();
        await processor.ProcessAsync(upload.IngestionJobId);

        AppDbContext? db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        DocumentChunk? chunk = await db.DocumentChunks.SingleAsync(x => x.DocumentId == upload.DocumentId);

        Assert.Equal("Integration DOCX text.", chunk.Text);
        Assert.Null(chunk.PageNumber);
    }

    [Fact]
    public async Task Uploaded_Pptx_Document_Should_Be_Processed_Into_Slide_Mapped_Chunks()
    {
        using HttpClient? client = factory.CreateClient();
        string? fileName = $"smoke-{Guid.NewGuid():N}.pptx";
        UploadDocumentResponse? upload = await UploadDocumentAsync(
            client,
            fileName,
            CreatePptxBytes("Integration PPTX text."),
            "application/vnd.openxmlformats-officedocument.presentationml.presentation");

        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        IIngestionJobProcessor? processor = scope.ServiceProvider.GetRequiredService<IIngestionJobProcessor>();
        await processor.ProcessAsync(upload.IngestionJobId);

        AppDbContext? db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        DocumentChunk? chunk = await db.DocumentChunks.SingleAsync(x => x.DocumentId == upload.DocumentId);

        Assert.Equal("Integration PPTX text.", chunk.Text);
        Assert.Equal(1, chunk.PageNumber);
    }

    private async Task ClearLastSelectedBucketAsync()
    {
        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        AppDbContext? db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        AppSetting[]? settings = await db.AppSettings
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
        using MultipartFormDataContent? form = new MultipartFormDataContent();
        using ByteArrayContent? file = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(content));
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        form.Add(file, "file", fileName);

        string? url = bucketId.HasValue ? $"/api/documents/upload?bucketId={bucketId}" : "/api/documents/upload";
        using HttpResponseMessage? uploadResponse = await client.PostAsync(url, form);
        Assert.Equal(HttpStatusCode.Created, uploadResponse.StatusCode);
        UploadDocumentResponse? upload = await uploadResponse.Content.ReadFromJsonAsync<UploadDocumentResponse>();
        Assert.NotNull(upload);
        return upload;
    }

    private static async Task<UploadDocumentResponse> UploadDocumentAsync(
        HttpClient client,
        string fileName,
        byte[] content,
        string contentType)
    {
        using MultipartFormDataContent? form = new MultipartFormDataContent();
        using ByteArrayContent? file = new ByteArrayContent(content);
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        form.Add(file, "file", fileName);

        using HttpResponseMessage? uploadResponse = await client.PostAsync("/api/documents/upload", form);
        Assert.Equal(HttpStatusCode.Created, uploadResponse.StatusCode);
        UploadDocumentResponse? upload = await uploadResponse.Content.ReadFromJsonAsync<UploadDocumentResponse>();
        Assert.NotNull(upload);
        return upload;
    }

    private static byte[] CreatePdfBytes(string text)
    {
        string? escapedText = text.Replace(@"\", @"\\", StringComparison.Ordinal)
            .Replace("(", @"\(", StringComparison.Ordinal)
            .Replace(")", @"\)", StringComparison.Ordinal);
        string? content = $"BT /F1 12 Tf 72 720 Td ({escapedText}) Tj ET";
        string[]? objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}\nendstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
        };
        StringBuilder? builder = new StringBuilder("%PDF-1.4\n");
        List<int>? offsets = new List<int> { 0 };
        for (int index = 0; index < objects.Length; index++)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(builder.ToString()));
            builder.Append(CultureInfo.InvariantCulture, $"{index + 1} 0 obj\n{objects[index]}\nendobj\n");
        }

        int xrefOffset = Encoding.ASCII.GetByteCount(builder.ToString());
        builder.Append(CultureInfo.InvariantCulture, $"xref\n0 {objects.Length + 1}\n");
        builder.Append("0000000000 65535 f \n");
        foreach (int offset in offsets.Skip(1))
        {
            builder.Append(CultureInfo.InvariantCulture, $"{offset:D10} 00000 n \n");
        }

        builder.Append(CultureInfo.InvariantCulture, $"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF");
        return Encoding.ASCII.GetBytes(builder.ToString());
    }

    private static byte[] CreateDocxBytes(string text)
    {
        using MemoryStream? stream = new MemoryStream();
        using (WordprocessingDocument? document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
        {
            MainDocumentPart? mainDocumentPart = document.AddMainDocumentPart();
            mainDocumentPart.Document = new W.Document(new W.Body(new W.Paragraph(new W.Run(new W.Text(text)))));
            mainDocumentPart.Document.Save();
        }

        return stream.ToArray();
    }

    private static byte[] CreatePptxBytes(string text)
    {
        using MemoryStream? stream = new MemoryStream();
        using (PresentationDocument? document = PresentationDocument.Create(stream, PresentationDocumentType.Presentation, true))
        {
            PresentationPart? presentationPart = document.AddPresentationPart();
            presentationPart.Presentation = new P.Presentation();
            SlidePart? slidePart = presentationPart.AddNewPart<SlidePart>("rId1");
            slidePart.Slide = new P.Slide(
                new P.CommonSlideData(
                    new P.ShapeTree(
                        new P.NonVisualGroupShapeProperties(
                            new P.NonVisualDrawingProperties { Id = 1U, Name = string.Empty },
                            new P.NonVisualGroupShapeDrawingProperties(),
                            new P.ApplicationNonVisualDrawingProperties()),
                        new P.GroupShapeProperties(new A.TransformGroup()),
                        new P.Shape(
                            new P.NonVisualShapeProperties(
                                new P.NonVisualDrawingProperties { Id = 2U, Name = "Text" },
                                new P.NonVisualShapeDrawingProperties(),
                                new P.ApplicationNonVisualDrawingProperties()),
                            new P.ShapeProperties(),
                            new P.TextBody(
                                new A.BodyProperties(),
                                new A.ListStyle(),
                                new A.Paragraph(new A.Run(new A.Text(text))))))));
            slidePart.Slide.Save();
            presentationPart.Presentation.AppendChild(new P.SlideIdList(new P.SlideId { Id = 256U, RelationshipId = "rId1" }));
            presentationPart.Presentation.Save();
        }

        return stream.ToArray();
    }
}
