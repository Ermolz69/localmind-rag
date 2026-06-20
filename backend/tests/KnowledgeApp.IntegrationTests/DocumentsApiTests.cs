using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Buckets;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Common;
using KnowledgeApp.Contracts.Documents;
using KnowledgeApp.Contracts.Ingestion;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Persistence;
using KnowledgeApp.Infrastructure.Services.DocumentPreview;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using A = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace KnowledgeApp.IntegrationTests;

public sealed class DocumentsApiTests : IClassFixture<LocalApiTestFactory>
{
    private readonly LocalApiTestFactory factory;

    public DocumentsApiTests(LocalApiTestFactory factory)
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

        CursorPage<DocumentDto>? documents =
            await client.GetApiDataAsync<CursorPage<DocumentDto>>("/api/v1/documents");

        Assert.Contains(
            documents?.Items ?? [],
            document => document.Id == upload.DocumentId && document.Name == fileName);

        DocumentDto? document =
            await client.GetApiDataAsync<DocumentDto>($"/api/v1/documents/{upload.DocumentId}");

        Assert.NotNull(document);
        Assert.Equal(fileName, document.Name);

        using IServiceScope? scope = factory.Services.CreateScope();

        AppDbContext? db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Document? storedDocument =
            await db.Documents.SingleAsync(x => x.Id == upload.DocumentId);

        LocalDevice? localDevice =
            await db.LocalDevices.SingleAsync(x => x.Id == storedDocument.LocalDeviceId);

        Bucket? defaultBucket =
            await db.Buckets.SingleAsync(x => x.Id == storedDocument.BucketId);

        DocumentFile? storedFile =
            await db.DocumentFiles.SingleAsync(x => x.DocumentId == upload.DocumentId);

        IngestionJob? ingestionJob =
            await db.IngestionJobs.SingleAsync(x => x.DocumentId == upload.DocumentId);

        Assert.Equal(BucketConstants.DefaultBucketName, defaultBucket.Name);
        Assert.False(string.IsNullOrWhiteSpace(localDevice.DeviceKey));
        Assert.Equal(fileName, storedFile.FileName);
        Assert.Equal(upload.IngestionJobId!.Value, ingestionJob.Id);
    }

    [Fact]
    public async Task UploadDocument_Should_Use_Selected_Bucket_And_Filter_By_Bucket()
    {
        using HttpClient? client = factory.CreateClient();

        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();

        AppDbContext? db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Bucket? selectedBucket = new Bucket
        {
            Name = $"Selected-{Guid.NewGuid():N}",
        };

        Bucket? otherBucket = new Bucket
        {
            Name = $"Other-{Guid.NewGuid():N}",
        };

        db.Buckets.AddRange(selectedBucket, otherBucket);

        await db.SaveChangesAsync();

        string? selectedFileName = $"selected-{Guid.NewGuid():N}.txt";
        string? otherFileName = $"other-{Guid.NewGuid():N}.txt";

        UploadDocumentResponse? selectedUpload =
            await UploadDocumentAsync(client, selectedFileName, selectedBucket.Id);

        UploadDocumentResponse? otherUpload =
            await UploadDocumentAsync(client, otherFileName, otherBucket.Id);

        CursorPage<DocumentDto>? filteredDocuments =
            await client.GetApiDataAsync<CursorPage<DocumentDto>>(
                $"/api/v1/documents?bucketId={selectedBucket.Id}");

        Assert.Contains(
            filteredDocuments?.Items ?? [],
            document => document.Id == selectedUpload.DocumentId);

        Assert.DoesNotContain(
            filteredDocuments?.Items ?? [],
            document => document.Id == otherUpload.DocumentId);

        await db.Entry(selectedBucket).ReloadAsync();
        await db.Entry(otherBucket).ReloadAsync();

        Document? selectedDocument =
            await db.Documents.SingleAsync(x => x.Id == selectedUpload.DocumentId);

        Assert.Equal(selectedBucket.Id, selectedDocument.BucketId);
    }

    [Fact]
    public async Task GetDocuments_Should_Return_Cursor_Page_And_Use_Next_Cursor()
    {
        using HttpClient client = factory.CreateClient();

        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();

        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Bucket bucket = new()
        {
            Name = $"Cursor-{Guid.NewGuid():N}",
        };

        db.Buckets.Add(bucket);

        await db.SaveChangesAsync();

        UploadDocumentResponse firstUpload =
            await UploadDocumentAsync(
                client,
                $"cursor-a-{Guid.NewGuid():N}.txt",
                bucket.Id);

        UploadDocumentResponse secondUpload =
            await UploadDocumentAsync(
                client,
                $"cursor-b-{Guid.NewGuid():N}.txt",
                bucket.Id);

        CursorPage<DocumentDto>? firstPage =
            await client.GetApiDataAsync<CursorPage<DocumentDto>>(
                $"/api/v1/documents?bucketId={bucket.Id}&limit=1");

        Assert.NotNull(firstPage);
        Assert.True(firstPage.HasMore);
        Assert.NotNull(firstPage.NextCursor);

        CursorPage<DocumentDto>? secondPage =
            await client.GetApiDataAsync<CursorPage<DocumentDto>>(
                $"/api/v1/documents?bucketId={bucket.Id}&limit=1&cursor={Uri.EscapeDataString(firstPage.NextCursor)}");

        Assert.NotNull(secondPage);

        Assert.DoesNotContain(
            secondPage.Items,
            document => document.Id == firstPage.Items[0].Id);

        Guid[] returnedDocumentIds = firstPage.Items
            .Concat(secondPage.Items)
            .Select(document => document.Id)
            .ToArray();

        Assert.Contains(firstUpload.DocumentId, returnedDocumentIds);
        Assert.Contains(secondUpload.DocumentId, returnedDocumentIds);
    }

    [Fact]
    public async Task GetDocumentById_Should_Return_NotFound_When_Document_Is_Missing()
    {
        using HttpClient? client = factory.CreateClient();

        using HttpResponseMessage? response =
            await client.GetAsync($"/api/v1/documents/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetDocumentPreview_Should_Return_Inline_Text_For_Text_Document()
    {
        using HttpClient client = factory.CreateClient();

        UploadDocumentResponse upload =
            await UploadDocumentAsync(
                client,
                $"preview-{Guid.NewGuid():N}.txt",
                content: "hello preview");

        DocumentPreviewResponse? preview =
            await client.GetApiDataAsync<DocumentPreviewResponse>(
                $"/api/v1/documents/{upload.DocumentId}/preview");

        Assert.NotNull(preview);
        Assert.Equal(upload.DocumentId, preview.DocumentId);
        Assert.Equal(DocumentPreviewKind.Text, preview.PreviewKind);
        Assert.Equal("hello preview", preview.TextContent);
        Assert.Null(preview.PreviewUrl);
        Assert.Null(preview.ErrorCode);
    }

    [Fact]
    public async Task GetDocumentPreview_Should_Return_Pdf_Preview_Url_And_Stream_File()
    {
        using HttpClient client = factory.CreateClient();

        byte[] pdfBytes = CreatePdfBytes("Preview PDF text.");
        UploadDocumentResponse upload =
            await UploadDocumentAsync(
                client,
                $"preview-{Guid.NewGuid():N}.pdf",
                pdfBytes,
                "application/pdf");

        DocumentPreviewResponse? preview =
            await client.GetApiDataAsync<DocumentPreviewResponse>(
                $"/api/v1/documents/{upload.DocumentId}/preview");

        Assert.NotNull(preview);
        Assert.Equal(DocumentPreviewKind.Pdf, preview.PreviewKind);
        Assert.Equal($"/api/v1/documents/{upload.DocumentId}/preview/file", preview.PreviewUrl);
        Assert.Null(preview.TextContent);

        using HttpResponseMessage response = await client.GetAsync(preview.PreviewUrl);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(pdfBytes, await response.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task GetDocumentPreview_Should_Return_Pdf_For_Converted_Docx_And_Use_Cache()
    {
        FakePreviewConverterProcess converter = new();
        using WebApplicationFactory<Program> conversionFactory =
            CreatePreviewConversionFactory(converter);

        using HttpClient client = conversionFactory.CreateClient();

        UploadDocumentResponse upload =
            await UploadDocumentAsync(
                client,
                $"preview-{Guid.NewGuid():N}.docx",
                CreateDocxBytes("Unsupported preview text."),
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        DocumentPreviewResponse? preview =
            await client.GetApiDataAsync<DocumentPreviewResponse>(
                $"/api/v1/documents/{upload.DocumentId}/preview");

        Assert.NotNull(preview);
        Assert.Equal(DocumentPreviewKind.Pdf, preview.PreviewKind);
        Assert.Equal("application/pdf", preview.ContentType);
        Assert.Equal($"/api/v1/documents/{upload.DocumentId}/preview/file", preview.PreviewUrl);
        Assert.Null(preview.ErrorCode);
        Assert.Null(preview.TextContent);

        using HttpResponseMessage response = await client.GetAsync(preview.PreviewUrl);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(FakePreviewConverterProcess.PdfBytes, await response.Content.ReadAsByteArrayAsync());

        DocumentPreviewResponse? cachedPreview =
            await client.GetApiDataAsync<DocumentPreviewResponse>(
                $"/api/v1/documents/{upload.DocumentId}/preview");

        Assert.NotNull(cachedPreview);
        Assert.Equal(DocumentPreviewKind.Pdf, cachedPreview.PreviewKind);
        Assert.Equal(1, converter.CallCount);
    }

    [Fact]
    public async Task GetDocumentPreview_Should_Return_Pdf_For_Converted_Pptx()
    {
        FakePreviewConverterProcess converter = new();
        using WebApplicationFactory<Program> conversionFactory =
            CreatePreviewConversionFactory(converter);

        using HttpClient client = conversionFactory.CreateClient();

        UploadDocumentResponse upload =
            await UploadDocumentAsync(
                client,
                $"preview-{Guid.NewGuid():N}.pptx",
                CreatePptxBytes("Converted PPTX preview text."),
                "application/vnd.openxmlformats-officedocument.presentationml.presentation");

        DocumentPreviewResponse? preview =
            await client.GetApiDataAsync<DocumentPreviewResponse>(
                $"/api/v1/documents/{upload.DocumentId}/preview");

        Assert.NotNull(preview);
        Assert.Equal(DocumentPreviewKind.Pdf, preview.PreviewKind);
        Assert.Equal("application/pdf", preview.ContentType);
        Assert.Equal($"/api/v1/documents/{upload.DocumentId}/preview/file", preview.PreviewUrl);
        Assert.Equal(1, converter.CallCount);
    }

    [Fact]
    public async Task GetDocumentPreview_Should_Return_Html_Fallback_When_Converter_Is_Unavailable()
    {
        // When LibreOffice is unavailable the handler falls back to the in-process
        // OOXML-to-HTML converter, so a valid .docx produces an Html response instead
        // of an error.
        FakePreviewConverterProcess converter = new(ApplicationErrors.ExternalDependency(
            ErrorCodes.Documents.PreviewConverterUnavailable,
            ErrorMessages.Documents.PreviewConverterUnavailable));
        using WebApplicationFactory<Program> conversionFactory =
            CreatePreviewConversionFactory(converter);

        using HttpClient client = conversionFactory.CreateClient();

        UploadDocumentResponse upload =
            await UploadDocumentAsync(
                client,
                $"preview-{Guid.NewGuid():N}.docx",
                CreateDocxBytes("Unavailable converter text."),
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        DocumentPreviewResponse? preview =
            await client.GetApiDataAsync<DocumentPreviewResponse>(
                $"/api/v1/documents/{upload.DocumentId}/preview");

        Assert.NotNull(preview);
        Assert.Equal(DocumentPreviewKind.Html, preview.PreviewKind);
        Assert.Null(preview.ErrorCode);
        Assert.Null(preview.PreviewUrl);
        Assert.NotNull(preview.TextContent);

        CursorPage<DocumentDto>? documents =
            await client.GetApiDataAsync<CursorPage<DocumentDto>>("/api/v1/documents");

        Assert.Contains(documents?.Items ?? [], document => document.Id == upload.DocumentId);
    }

    [Fact]
    public async Task GetDocumentPreview_Should_Return_NotFound_When_Document_Is_Missing()
    {
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response =
            await client.GetAsync($"/api/v1/documents/{Guid.NewGuid()}/preview");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        ApiResponse<object?> envelope =
            await response.Content.ReadApiErrorAsync();

        Assert.Equal("DOCUMENT_NOT_FOUND", envelope.Error!.Code);
    }

    [Fact]
    public async Task GetDocumentPreviewFile_Should_Not_Stream_Unmanaged_File_Path()
    {
        using HttpClient client = factory.CreateClient();

        UploadDocumentResponse upload =
            await UploadDocumentAsync(
                client,
                $"preview-{Guid.NewGuid():N}.pdf",
                CreatePdfBytes("Managed file only."),
                "application/pdf");

        string externalPath = Path.Combine(
            Path.GetTempPath(),
            $"localmind-preview-{Guid.NewGuid():N}.pdf");

        await File.WriteAllTextAsync(externalPath, "external content");

        try
        {
            await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
            AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            DocumentFile file =
                await db.DocumentFiles.SingleAsync(x => x.DocumentId == upload.DocumentId);

            file.LocalPath = externalPath;
            await db.SaveChangesAsync();

            DocumentPreviewResponse? preview =
                await client.GetApiDataAsync<DocumentPreviewResponse>(
                    $"/api/v1/documents/{upload.DocumentId}/preview");

            Assert.NotNull(preview);
            Assert.Equal(DocumentPreviewKind.Error, preview.PreviewKind);
            Assert.Equal("DOCUMENT_PREVIEW_FILE_MISSING", preview.ErrorCode);
            Assert.Null(preview.PreviewUrl);
            Assert.Null(preview.TextContent);

            using HttpResponseMessage response =
                await client.GetAsync($"/api/v1/documents/{upload.DocumentId}/preview/file");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
        finally
        {
            if (File.Exists(externalPath))
            {
                File.Delete(externalPath);
            }
        }
    }

    [Fact]
    public async Task GetDocumentPreview_Should_Return_Controlled_Error_For_Corrupt_Text_File()
    {
        using HttpClient client = factory.CreateClient();

        UploadDocumentResponse upload =
            await UploadDocumentAsync(
                client,
                $"preview-{Guid.NewGuid():N}.txt",
                content: "valid before corruption");

        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        DocumentFile file =
            await db.DocumentFiles.SingleAsync(x => x.DocumentId == upload.DocumentId);

        await File.WriteAllBytesAsync(file.LocalPath, [0xC3, 0x28]);

        DocumentPreviewResponse? preview =
            await client.GetApiDataAsync<DocumentPreviewResponse>(
                $"/api/v1/documents/{upload.DocumentId}/preview");

        Assert.NotNull(preview);
        Assert.Equal(DocumentPreviewKind.Error, preview.PreviewKind);
        Assert.Equal("DOCUMENT_PREVIEW_UNAVAILABLE", preview.ErrorCode);
        Assert.Null(preview.PreviewUrl);
        Assert.Null(preview.TextContent);
    }

    [Fact]
    public async Task UploadDocument_Should_Return_ValidationProblemDetails_For_Unsupported_File()
    {
        using HttpClient? client = factory.CreateClient();
        using MultipartFormDataContent? form = new MultipartFormDataContent();
        using ByteArrayContent? file = new ByteArrayContent("unsupported"u8.ToArray());

        file.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

        form.Add(file, "file", "unsupported.exe");

        using HttpResponseMessage? response =
            await client.PostAsync("/api/v1/documents/upload", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        ApiResponse<object?> envelope =
            await response.Content.ReadApiErrorAsync();

        Assert.Equal("VALIDATION_FAILED", envelope.Error!.Code);

        Assert.Contains(
            envelope.Error.Details ?? [],
            detail => detail.Field == "fileName");
    }

    [Fact]
    public async Task UploadDocument_Should_Return_ProblemDetails_For_Missing_Selected_Bucket()
    {
        using HttpClient? client = factory.CreateClient();
        using MultipartFormDataContent? form = new MultipartFormDataContent();
        using ByteArrayContent? file = new ByteArrayContent("content"u8.ToArray());

        file.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");

        form.Add(file, "file", "notes.txt");

        using HttpResponseMessage? response =
            await client.PostAsync(
                $"/api/v1/documents/upload?bucketId={Guid.NewGuid()}",
                form);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        ApiResponse<object?> envelope =
            await response.Content.ReadApiErrorAsync();

        Assert.Equal("BUCKET_NOT_FOUND", envelope.Error!.Code);
    }

    [Fact]
    public async Task Uploaded_Text_Document_Should_Be_Processed_Into_Chunks()
    {
        using HttpClient? client = factory.CreateClient();

        string? fileName = $"ingestion-{Guid.NewGuid():N}.txt";

        UploadDocumentResponse? upload =
            await UploadDocumentAsync(
                client,
                fileName,
                content: "First paragraph.\n\nSecond paragraph.");

        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();

        IIngestionJobProcessor? processor =
            scope.ServiceProvider.GetRequiredService<IIngestionJobProcessor>();

        await processor.ProcessAsync(upload.IngestionJobId!.Value);

        AppDbContext? db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        DocumentChunk[]? chunks = await db.DocumentChunks
            .Where(x => x.DocumentId == upload.DocumentId)
            .OrderBy(x => x.Index)
            .ToArrayAsync();

        Document? document =
            await db.Documents.SingleAsync(x => x.Id == upload.DocumentId);

        IngestionJob? job =
            await db.IngestionJobs.SingleAsync(x => x.Id == upload.IngestionJobId!.Value);

        Assert.Single(chunks);
        Assert.Equal("First paragraph.\n\nSecond paragraph.", chunks[0].Text);
        Assert.Equal(DocumentStatus.Indexed, document.Status);
        Assert.Equal(IngestionJobStatus.Indexed, job.Status);
        Assert.Equal(100, job.ProgressPercent);
        Assert.Equal("Indexed", job.CurrentStep);

        DocumentEmbedding? embedding =
            await db.DocumentEmbeddings.SingleAsync(
                x => x.DocumentChunkId == chunks[0].Id);

        Assert.Equal("BGE-M3", embedding.ModelName);
        Assert.Equal(32, embedding.Dimension);
        Assert.Equal(32 * sizeof(float), embedding.Embedding.Length);
    }

    [Fact]
    public async Task Uploaded_Text_Document_Should_Be_Automatically_Processed_By_Worker()
    {
        using WebApplicationFactory<Program> autoWorkerFactory =
            factory.WithWebHostBuilder(builder =>
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    Dictionary<string, string?> settings = new()
                    {
                        ["IngestionWorker:Enabled"] = "true",
                        ["IngestionWorker:RecoveryIntervalSeconds"] = "60",
                        ["IngestionWorker:RecoveryBatchSize"] = "100",
                    };

                    configuration.AddInMemoryCollection(settings);
                }));

        using HttpClient client = autoWorkerFactory.CreateClient();

        string fileName = $"auto-ingestion-{Guid.NewGuid():N}.txt";

        UploadDocumentResponse upload =
            await UploadDocumentAsync(
                client,
                fileName,
                content: "Automatic worker paragraph.");

        await WaitForAsync(async () =>
        {
            await using AsyncServiceScope scope =
                autoWorkerFactory.Services.CreateAsyncScope();

            AppDbContext db =
                scope.ServiceProvider.GetRequiredService<AppDbContext>();

            IngestionJob job =
                await db.IngestionJobs.SingleAsync(
                    x => x.Id == upload.IngestionJobId!.Value);

            return job.Status == IngestionJobStatus.Indexed;
        });

        await using AsyncServiceScope verificationScope =
            autoWorkerFactory.Services.CreateAsyncScope();

        AppDbContext verificationDb =
            verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();

        DocumentChunk chunk =
            await verificationDb.DocumentChunks.SingleAsync(
                x => x.DocumentId == upload.DocumentId);

        Document document =
            await verificationDb.Documents.SingleAsync(
                x => x.Id == upload.DocumentId);

        IngestionJob completedJob =
            await verificationDb.IngestionJobs.SingleAsync(
                x => x.Id == upload.IngestionJobId!.Value);

        Assert.Equal("Automatic worker paragraph.", chunk.Text);
        Assert.Equal(DocumentStatus.Indexed, document.Status);
        Assert.Equal(IngestionJobStatus.Indexed, completedJob.Status);
        Assert.Equal(100, completedJob.ProgressPercent);
    }

    [Fact]
    public async Task Uploaded_Corrupt_Pdf_Document_Should_Be_Automatically_Failed_By_Worker()
    {
        using WebApplicationFactory<Program> autoWorkerFactory =
            factory.WithWebHostBuilder(builder =>
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    Dictionary<string, string?> settings = new()
                    {
                        ["IngestionWorker:Enabled"] = "true",
                        ["IngestionWorker:RecoveryIntervalSeconds"] = "60",
                        ["IngestionWorker:RecoveryBatchSize"] = "100",
                    };

                    configuration.AddInMemoryCollection(settings);
                }));

        using HttpClient client = autoWorkerFactory.CreateClient();

        string fileName = $"auto-failed-{Guid.NewGuid():N}.pdf";

        UploadDocumentResponse upload =
            await UploadDocumentAsync(
                client,
                fileName,
                content: "%PDF skeleton",
                contentType: "application/pdf");

        await WaitForAsync(async () =>
        {
            await using AsyncServiceScope scope =
                autoWorkerFactory.Services.CreateAsyncScope();

            AppDbContext db =
                scope.ServiceProvider.GetRequiredService<AppDbContext>();

            IngestionJob job =
                await db.IngestionJobs.SingleAsync(
                    x => x.Id == upload.IngestionJobId!.Value);

            Document doc =
                await db.Documents.SingleAsync(
                    x => x.Id == upload.DocumentId);

            return job.Status == IngestionJobStatus.Failed && doc.Status == DocumentStatus.Failed;
        });

        await using AsyncServiceScope verificationScope =
            autoWorkerFactory.Services.CreateAsyncScope();

        AppDbContext verificationDb =
            verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();

        Document document =
            await verificationDb.Documents.SingleAsync(
                x => x.Id == upload.DocumentId);

        IngestionJob failedJob =
            await verificationDb.IngestionJobs.SingleAsync(
                x => x.Id == upload.IngestionJobId!.Value);

        Assert.Equal(DocumentStatus.Failed, document.Status);
        Assert.Equal(IngestionJobStatus.Failed, failedJob.Status);
        Assert.Equal("INGESTION_JOB_FAILED", failedJob.ErrorCode);
        Assert.False(string.IsNullOrWhiteSpace(failedJob.ErrorMessage));
    }

    [Fact]
    public async Task Recovery_Timer_Should_Process_Pending_Job_Created_Without_Signal()
    {
        using WebApplicationFactory<Program> autoWorkerFactory =
            factory.WithWebHostBuilder(builder =>
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    Dictionary<string, string?> settings = new()
                    {
                        ["IngestionWorker:Enabled"] = "true",
                        ["IngestionWorker:RecoveryIntervalSeconds"] = "1",
                        ["IngestionWorker:RecoveryBatchSize"] = "100",
                    };

                    configuration.AddInMemoryCollection(settings);
                }));

        using HttpClient client = autoWorkerFactory.CreateClient();
        await Task.Delay(250);

        Guid jobId;
        await using (AsyncServiceScope scope =
            autoWorkerFactory.Services.CreateAsyncScope())
        {
            AppDbContext db =
                scope.ServiceProvider.GetRequiredService<AppDbContext>();
            IngestionJob job = new()
            {
                DocumentId = Guid.NewGuid(),
                Status = IngestionJobStatus.Pending,
                CurrentStep = "Pending",
                ProgressPercent = 0,
            };
            db.IngestionJobs.Add(job);
            await db.SaveChangesAsync();
            jobId = job.Id;
        }

        await WaitForAsync(async () =>
        {
            await using AsyncServiceScope scope =
                autoWorkerFactory.Services.CreateAsyncScope();
            AppDbContext db =
                scope.ServiceProvider.GetRequiredService<AppDbContext>();
            IngestionJob job =
                await db.IngestionJobs.SingleAsync(x => x.Id == jobId);
            return job.Status == IngestionJobStatus.Failed;
        });

        await using AsyncServiceScope verificationScope =
            autoWorkerFactory.Services.CreateAsyncScope();
        AppDbContext verificationDb =
            verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        IngestionJob recoveredJob =
            await verificationDb.IngestionJobs.SingleAsync(x => x.Id == jobId);

        Assert.Equal(IngestionJobStatus.Failed, recoveredJob.Status);
        Assert.Equal("INGESTION_JOB_FAILED", recoveredJob.ErrorCode);
    }

    [Fact]
    public async Task ProcessIngestionJobEndpoint_Should_Process_Uploaded_Text_Document()
    {
        using HttpClient? client = factory.CreateClient();

        string? fileName = $"endpoint-ingestion-{Guid.NewGuid():N}.txt";

        UploadDocumentResponse? upload =
            await UploadDocumentAsync(
                client,
                fileName,
                content: "Endpoint paragraph.");

        using HttpResponseMessage? response =
            await client.PostAsync(
                $"/api/v1/ingestion/jobs/{upload.IngestionJobId!.Value}/process",
                content: null);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        Assert.Equal(
            $"/api/v1/ingestion/jobs/{upload.IngestionJobId!.Value}",
            response.Headers.Location!.OriginalString);

        ProcessIngestionJobResponse? body =
            await response.Content.ReadApiDataAsync<ProcessIngestionJobResponse>();

        Assert.NotNull(body);
        Assert.Equal(upload.IngestionJobId!.Value, body.JobId);

        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();

        AppDbContext? db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        DocumentChunk? chunk =
            await db.DocumentChunks.SingleAsync(
                x => x.DocumentId == upload.DocumentId);

        Document? document =
            await db.Documents.SingleAsync(
                x => x.Id == upload.DocumentId);

        Assert.Equal("Endpoint paragraph.", chunk.Text);
        Assert.Equal(DocumentStatus.Indexed, document.Status);
    }

    [Fact]
    public async Task Uploaded_Corrupt_Pdf_Document_Should_Fail_Ingestion()
    {
        using HttpClient? client = factory.CreateClient();

        string? fileName = $"unsupported-{Guid.NewGuid():N}.pdf";

        UploadDocumentResponse? upload =
            await UploadDocumentAsync(
                client,
                fileName,
                content: "%PDF skeleton",
                contentType: "application/pdf");

        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();

        IIngestionJobProcessor? processor =
            scope.ServiceProvider.GetRequiredService<IIngestionJobProcessor>();

        await processor.ProcessAsync(upload.IngestionJobId!.Value);

        AppDbContext? db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Document? document =
            await db.Documents.SingleAsync(x => x.Id == upload.DocumentId);

        IngestionJob? job =
            await db.IngestionJobs.SingleAsync(x => x.Id == upload.IngestionJobId!.Value);

        Assert.Equal(DocumentStatus.Failed, document.Status);
        Assert.Equal(IngestionJobStatus.Failed, job.Status);
        Assert.Equal("INGESTION_JOB_FAILED", job.ErrorCode);
        Assert.Contains("PDF", job.ErrorMessage, StringComparison.OrdinalIgnoreCase);

        CursorPage<DocumentDto>? documents =
            await client.GetApiDataAsync<CursorPage<DocumentDto>>("/api/v1/documents");

        Assert.Contains(
            documents?.Items ?? [],
            item => item.Id == upload.DocumentId && item.LastError != null);
    }

    [Fact]
    public async Task Uploaded_Pdf_Document_Should_Be_Processed_Into_Page_Mapped_Chunks()
    {
        using HttpClient? client = factory.CreateClient();

        string? fileName = $"smoke-{Guid.NewGuid():N}.pdf";

        UploadDocumentResponse? upload =
            await UploadDocumentAsync(
                client,
                fileName,
                CreatePdfBytes("Integration PDF text."),
                "application/pdf");

        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();

        IIngestionJobProcessor? processor =
            scope.ServiceProvider.GetRequiredService<IIngestionJobProcessor>();

        await processor.ProcessAsync(upload.IngestionJobId!.Value);

        AppDbContext? db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        DocumentChunk? chunk =
            await db.DocumentChunks.SingleAsync(
                x => x.DocumentId == upload.DocumentId);

        Document? document =
            await db.Documents.SingleAsync(
                x => x.Id == upload.DocumentId);

        Assert.Contains(
            "Integration PDF text.",
            chunk.Text,
            StringComparison.OrdinalIgnoreCase);

        Assert.Equal(1, chunk.PageNumber);
        Assert.Equal(DocumentStatus.Indexed, document.Status);
    }

    [Fact]
    public async Task Uploaded_Docx_Document_Should_Be_Processed_Into_Chunks()
    {
        using HttpClient? client = factory.CreateClient();

        string? fileName = $"smoke-{Guid.NewGuid():N}.docx";

        UploadDocumentResponse? upload =
            await UploadDocumentAsync(
                client,
                fileName,
                CreateDocxBytes("Integration DOCX text."),
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document");

        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();

        IIngestionJobProcessor? processor =
            scope.ServiceProvider.GetRequiredService<IIngestionJobProcessor>();

        await processor.ProcessAsync(upload.IngestionJobId!.Value);

        AppDbContext? db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        DocumentChunk? chunk =
            await db.DocumentChunks.SingleAsync(
                x => x.DocumentId == upload.DocumentId);

        Assert.Equal("Integration DOCX text.", chunk.Text);
        Assert.Null(chunk.PageNumber);
    }

    [Fact]
    public async Task Uploaded_Pptx_Document_Should_Be_Processed_Into_Slide_Mapped_Chunks()
    {
        using HttpClient? client = factory.CreateClient();

        string? fileName = $"smoke-{Guid.NewGuid():N}.pptx";

        UploadDocumentResponse? upload =
            await UploadDocumentAsync(
                client,
                fileName,
                CreatePptxBytes("Integration PPTX text."),
                "application/vnd.openxmlformats-officedocument.presentationml.presentation");

        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();

        IIngestionJobProcessor? processor =
            scope.ServiceProvider.GetRequiredService<IIngestionJobProcessor>();

        await processor.ProcessAsync(upload.IngestionJobId!.Value);

        AppDbContext? db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        DocumentChunk? chunk =
            await db.DocumentChunks.SingleAsync(
                x => x.DocumentId == upload.DocumentId);

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

    private WebApplicationFactory<Program> CreatePreviewConversionFactory(
        FakePreviewConverterProcess converter)
    {
        return factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IDocumentPreviewConverterProcess>();
                services.AddSingleton<IDocumentPreviewConverterProcess>(converter);
            }));
    }

    private async Task<UploadDocumentResponse> UploadDocumentAsync(
        HttpClient client,
        string fileName,
        Guid? bucketId = null,
        string content = "hello from integration test",
        string contentType = "text/plain")
    {
        using MultipartFormDataContent? form = new MultipartFormDataContent();

        using ByteArrayContent? file =
            new ByteArrayContent(Encoding.UTF8.GetBytes(content));

        file.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

        form.Add(file, "file", fileName);

        string? url = bucketId.HasValue
            ? $"/api/v1/documents/upload?bucketId={bucketId}"
            : "/api/v1/documents/upload";

        using HttpResponseMessage? uploadResponse =
            await client.PostAsync(url, form);

        Assert.Equal(HttpStatusCode.Created, uploadResponse.StatusCode);
        Assert.NotNull(uploadResponse.Headers.Location);

        Assert.StartsWith(
            "/api/v1/documents/",
            uploadResponse.Headers.Location!.OriginalString,
            StringComparison.Ordinal);

        UploadDocumentResponse? upload =
            await uploadResponse.Content.ReadApiDataAsync<UploadDocumentResponse>();

        Assert.NotNull(upload);

        using IServiceScope scope = factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var job = await db.IngestionJobs.FirstOrDefaultAsync(j => j.DocumentId == upload.DocumentId);

        return upload with { IngestionJobId = job?.Id };
    }

    private async Task<UploadDocumentResponse> UploadDocumentAsync(
        HttpClient client,
        string fileName,
        byte[] content,
        string contentType)
    {
        using MultipartFormDataContent? form = new MultipartFormDataContent();
        using ByteArrayContent? file = new ByteArrayContent(content);

        file.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

        form.Add(file, "file", fileName);

        using HttpResponseMessage? uploadResponse =
            await client.PostAsync("/api/v1/documents/upload", form);

        Assert.Equal(HttpStatusCode.Created, uploadResponse.StatusCode);
        Assert.NotNull(uploadResponse.Headers.Location);

        Assert.StartsWith(
            "/api/v1/documents/",
            uploadResponse.Headers.Location!.OriginalString,
            StringComparison.Ordinal);

        UploadDocumentResponse? upload =
            await uploadResponse.Content.ReadApiDataAsync<UploadDocumentResponse>();

        Assert.NotNull(upload);

        using IServiceScope scope = factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var job = await db.IngestionJobs.FirstOrDefaultAsync(j => j.DocumentId == upload.DocumentId);

        return upload with { IngestionJobId = job?.Id };
    }

    private static async Task WaitForAsync(Func<Task<bool>> condition)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddSeconds(10);

        while (DateTimeOffset.UtcNow < deadline)
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250));
        }

        throw new TimeoutException("Timed out waiting for condition.");
    }

    private static byte[] CreatePdfBytes(string text)
    {
        string? escapedText = text
            .Replace(@"\", @"\\", StringComparison.Ordinal)
            .Replace("(", @"\(", StringComparison.Ordinal)
            .Replace(")", @"\)", StringComparison.Ordinal);

        string? content = $"BT /F1 12 Tf 72 720 Td ({escapedText}) Tj ET";

        string[]? objects =
        [
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}\nendstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
        ];

        StringBuilder? builder = new StringBuilder("%PDF-1.4\n");

        List<int>? offsets = [0];

        for (int index = 0; index < objects.Length; index++)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(builder.ToString()));

            builder.Append(
                CultureInfo.InvariantCulture,
                $"{index + 1} 0 obj\n{objects[index]}\nendobj\n");
        }

        int xrefOffset = Encoding.ASCII.GetByteCount(builder.ToString());

        builder.Append(
            CultureInfo.InvariantCulture,
            $"xref\n0 {objects.Length + 1}\n");

        builder.Append("0000000000 65535 f \n");

        foreach (int offset in offsets.Skip(1))
        {
            builder.Append(
                CultureInfo.InvariantCulture,
                $"{offset:D10} 00000 n \n");
        }

        builder.Append(
            CultureInfo.InvariantCulture,
            $"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF");

        return Encoding.ASCII.GetBytes(builder.ToString());
    }

    private sealed class FakePreviewConverterProcess(ApplicationError? error = null) : IDocumentPreviewConverterProcess
    {
        public static readonly byte[] PdfBytes = CreatePdfBytes("Converted preview.");

        public int CallCount { get; private set; }

        public async Task<Result<DocumentPreviewProcessResult>> ConvertToPdfAsync(
            string sourcePath,
            string outputDirectory,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            CallCount++;

            if (error is not null)
            {
                return Result<DocumentPreviewProcessResult>.Failure(error);
            }

            Directory.CreateDirectory(outputDirectory);
            string pdfPath = Path.Combine(outputDirectory, "converted.pdf");
            await File.WriteAllBytesAsync(pdfPath, PdfBytes, cancellationToken);

            return Result<DocumentPreviewProcessResult>.Success(new DocumentPreviewProcessResult(pdfPath));
        }
    }

    private static byte[] CreateDocxBytes(string text)
    {
        using MemoryStream? stream = new MemoryStream();

        using (
            WordprocessingDocument? document =
                WordprocessingDocument.Create(
                    stream,
                    WordprocessingDocumentType.Document,
                    true))
        {
            MainDocumentPart? mainDocumentPart =
                document.AddMainDocumentPart();

            mainDocumentPart.Document =
                new W.Document(
                    new W.Body(
                        new W.Paragraph(
                            new W.Run(
                                new W.Text(text)))));

            mainDocumentPart.Document.Save();
        }

        return stream.ToArray();
    }

    private static byte[] CreatePptxBytes(string text)
    {
        using MemoryStream? stream = new MemoryStream();

        using (
            PresentationDocument? document =
                PresentationDocument.Create(
                    stream,
                    PresentationDocumentType.Presentation,
                    true))
        {
            PresentationPart? presentationPart =
                document.AddPresentationPart();

            presentationPart.Presentation = new P.Presentation();

            SlidePart? slidePart =
                presentationPart.AddNewPart<SlidePart>("rId1");

            slidePart.Slide = new P.Slide(
                new P.CommonSlideData(
                    new P.ShapeTree(
                        new P.NonVisualGroupShapeProperties(
                            new P.NonVisualDrawingProperties
                            {
                                Id = 1U,
                                Name = string.Empty,
                            },
                            new P.NonVisualGroupShapeDrawingProperties(),
                            new P.ApplicationNonVisualDrawingProperties()),
                        new P.GroupShapeProperties(
                            new A.TransformGroup()),
                        new P.Shape(
                            new P.NonVisualShapeProperties(
                                new P.NonVisualDrawingProperties
                                {
                                    Id = 2U,
                                    Name = "Text",
                                },
                                new P.NonVisualShapeDrawingProperties(),
                                new P.ApplicationNonVisualDrawingProperties()),
                            new P.ShapeProperties(),
                            new P.TextBody(
                                new A.BodyProperties(),
                                new A.ListStyle(),
                                new A.Paragraph(
                                    new A.Run(
                                        new A.Text(text))))))));

            slidePart.Slide.Save();

            presentationPart.Presentation.AppendChild(
                new P.SlideIdList(
                    new P.SlideId
                    {
                        Id = 256U,
                        RelationshipId = "rId1",
                    }));

            presentationPart.Presentation.Save();
        }

        return stream.ToArray();
    }
}
