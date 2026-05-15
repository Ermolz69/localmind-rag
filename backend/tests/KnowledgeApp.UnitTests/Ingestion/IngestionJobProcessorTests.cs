using System.Globalization;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Persistence;
using KnowledgeApp.Infrastructure.Services;
using KnowledgeApp.Infrastructure.Options;
using Microsoft.Extensions.Options;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using A = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace KnowledgeApp.UnitTests.Ingestion;

public sealed class IngestionJobProcessorTests : IAsyncDisposable
{
    private readonly List<string> filesToDelete = [];

    [Fact]
    public async Task ProcessAsync_Should_Index_Text_Document_And_Complete_Job()
    {
        await using var database = await TestDatabase.CreateAsync();
        var document = await CreateDocumentWithJobAsync(database, "notes.txt", FileType.PlainText, "First paragraph.\n\nSecond paragraph.");
        var processor = CreateProcessor(database);

        await processor.ProcessAsync(document.JobId);

        var chunks = await database.Context.DocumentChunks
            .Where(x => x.DocumentId == document.DocumentId)
            .OrderBy(x => x.Index)
            .ToArrayAsync();
        var storedDocument = await database.Context.Documents.SingleAsync(x => x.Id == document.DocumentId);
        var job = await database.Context.IngestionJobs.SingleAsync(x => x.Id == document.JobId);

        Assert.Single(chunks);
        Assert.Equal("First paragraph.\n\nSecond paragraph.", chunks[0].Text);
        Assert.Equal(DocumentStatus.Indexed, storedDocument.Status);
        Assert.Equal(IngestionJobStatus.Completed, job.Status);
        Assert.Null(job.LastError);
        Assert.NotNull(job.ProcessedAt);

        var embedding = await database.Context.DocumentEmbeddings.SingleAsync(x => x.DocumentChunkId == chunks[0].Id);
        Assert.Equal("BGE-M3", embedding.ModelName);
        Assert.Equal(32, embedding.Dimension);
        Assert.Equal(32 * sizeof(float), embedding.Embedding.Length);
    }

    [Fact]
    public async Task ProcessAsync_Should_Remove_Existing_Chunks_And_Embeddings_On_Reindex()
    {
        await using var database = await TestDatabase.CreateAsync();
        var document = await CreateDocumentWithJobAsync(database, "notes.md", FileType.Markdown, "New content.");
        var oldChunk = new DocumentChunk
        {
            DocumentId = document.DocumentId,
            Index = 0,
            Text = "Old content.",
        };
        database.Context.DocumentChunks.Add(oldChunk);
        database.Context.DocumentEmbeddings.Add(new DocumentEmbedding
        {
            DocumentChunkId = oldChunk.Id,
            ModelName = "BGE-M3",
            Dimension = 2,
            Embedding = new byte[2 * sizeof(float)],
        });
        await database.Context.SaveChangesAsync();
        var processor = CreateProcessor(database);

        await processor.ProcessAsync(document.JobId);

        var chunks = await database.Context.DocumentChunks
            .Where(x => x.DocumentId == document.DocumentId)
            .ToArrayAsync();
        var embeddings = await database.Context.DocumentEmbeddings.ToArrayAsync();

        Assert.Single(chunks);
        Assert.Equal("New content.", chunks[0].Text);
        Assert.NotEqual(oldChunk.Id, chunks[0].Id);
        var embedding = Assert.Single(embeddings);
        Assert.Equal(chunks[0].Id, embedding.DocumentChunkId);
        Assert.Equal(32, embedding.Dimension);
    }

    [Fact]
    public async Task ProcessAsync_Should_Index_Pdf_Document_And_Complete_Job()
    {
        await using var database = await TestDatabase.CreateAsync();
        var document = await CreateDocumentWithJobAsync(database, "paper.pdf", FileType.Pdf, CreatePdfBytes("First PDF paragraph."));
        var processor = CreateProcessor(database);

        await processor.ProcessAsync(document.JobId);

        var chunks = await database.Context.DocumentChunks
            .Where(x => x.DocumentId == document.DocumentId)
            .OrderBy(x => x.Index)
            .ToArrayAsync();
        var storedDocument = await database.Context.Documents.SingleAsync(x => x.Id == document.DocumentId);
        var job = await database.Context.IngestionJobs.SingleAsync(x => x.Id == document.JobId);

        Assert.Single(chunks);
        Assert.Contains("First PDF paragraph.", chunks[0].Text, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, chunks[0].PageNumber);
        Assert.Equal(DocumentStatus.Indexed, storedDocument.Status);
        Assert.Equal(IngestionJobStatus.Completed, job.Status);
        Assert.Null(job.LastError);
    }

    [Fact]
    public async Task ProcessAsync_Should_Index_Docx_Document_And_Complete_Job()
    {
        await using var database = await TestDatabase.CreateAsync();
        var document = await CreateDocumentWithJobAsync(database, "document.docx", FileType.Docx, CreateDocxBytes("First DOCX paragraph."));
        var processor = CreateProcessor(database);

        await processor.ProcessAsync(document.JobId);

        var chunk = await database.Context.DocumentChunks.SingleAsync(x => x.DocumentId == document.DocumentId);
        var storedDocument = await database.Context.Documents.SingleAsync(x => x.Id == document.DocumentId);
        var job = await database.Context.IngestionJobs.SingleAsync(x => x.Id == document.JobId);

        Assert.Equal("First DOCX paragraph.", chunk.Text);
        Assert.Null(chunk.PageNumber);
        Assert.Equal(DocumentStatus.Indexed, storedDocument.Status);
        Assert.Equal(IngestionJobStatus.Completed, job.Status);
        Assert.Null(job.LastError);
    }

    [Fact]
    public async Task ProcessAsync_Should_Index_Pptx_Document_And_Complete_Job()
    {
        await using var database = await TestDatabase.CreateAsync();
        var document = await CreateDocumentWithJobAsync(database, "slides.pptx", FileType.Pptx, CreatePptxBytes("First PPTX paragraph."));
        var processor = CreateProcessor(database);

        await processor.ProcessAsync(document.JobId);

        var chunk = await database.Context.DocumentChunks.SingleAsync(x => x.DocumentId == document.DocumentId);
        var storedDocument = await database.Context.Documents.SingleAsync(x => x.Id == document.DocumentId);
        var job = await database.Context.IngestionJobs.SingleAsync(x => x.Id == document.JobId);

        Assert.Equal("First PPTX paragraph.", chunk.Text);
        Assert.Equal(1, chunk.PageNumber);
        Assert.Equal(DocumentStatus.Indexed, storedDocument.Status);
        Assert.Equal(IngestionJobStatus.Completed, job.Status);
        Assert.Null(job.LastError);
    }

    [Fact]
    public async Task ProcessAsync_Should_Fail_Corrupt_Pdf_Document()
    {
        await using var database = await TestDatabase.CreateAsync();
        var document = await CreateDocumentWithJobAsync(database, "paper.pdf", FileType.Pdf, Encoding.UTF8.GetBytes("%PDF skeleton"));
        var processor = CreateProcessor(database);

        await processor.ProcessAsync(document.JobId);

        var storedDocument = await database.Context.Documents.SingleAsync(x => x.Id == document.DocumentId);
        var job = await database.Context.IngestionJobs.SingleAsync(x => x.Id == document.JobId);

        Assert.Equal(DocumentStatus.Failed, storedDocument.Status);
        Assert.Equal(IngestionJobStatus.Failed, job.Status);
        Assert.Contains("PDF", job.LastError, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcessAsync_Should_Fail_Corrupt_Docx_Document()
    {
        await using var database = await TestDatabase.CreateAsync();
        var document = await CreateDocumentWithJobAsync(database, "document.docx", FileType.Docx, Encoding.UTF8.GetBytes("not a docx"));
        var processor = CreateProcessor(database);

        await processor.ProcessAsync(document.JobId);

        var storedDocument = await database.Context.Documents.SingleAsync(x => x.Id == document.DocumentId);
        var job = await database.Context.IngestionJobs.SingleAsync(x => x.Id == document.JobId);

        Assert.Equal(DocumentStatus.Failed, storedDocument.Status);
        Assert.Equal(IngestionJobStatus.Failed, job.Status);
        Assert.NotNull(job.LastError);
    }

    [Fact]
    public async Task ProcessAsync_Should_Fail_Corrupt_Pptx_Document()
    {
        await using var database = await TestDatabase.CreateAsync();
        var document = await CreateDocumentWithJobAsync(database, "slides.pptx", FileType.Pptx, Encoding.UTF8.GetBytes("not a pptx"));
        var processor = CreateProcessor(database);

        await processor.ProcessAsync(document.JobId);

        var storedDocument = await database.Context.Documents.SingleAsync(x => x.Id == document.DocumentId);
        var job = await database.Context.IngestionJobs.SingleAsync(x => x.Id == document.JobId);

        Assert.Equal(DocumentStatus.Failed, storedDocument.Status);
        Assert.Equal(IngestionJobStatus.Failed, job.Status);
        Assert.NotNull(job.LastError);
    }

    [Fact]
    public async Task ProcessAsync_Should_Throw_When_Job_Is_Missing()
    {
        await using var database = await TestDatabase.CreateAsync();
        var processor = CreateProcessor(database);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => processor.ProcessAsync(Guid.NewGuid()));

        Assert.Equal("Ingestion job was not found.", exception.Message);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var filePath in filesToDelete)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        await Task.CompletedTask;
    }

    private static IngestionJobProcessor CreateProcessor(TestDatabase database)
    {
        var rawExtractor = new RawTextExtractor();
        return new IngestionJobProcessor(
            database.Context,
            new DocumentTextExtractorFactory(rawExtractor, new HtmlTextExtractor(), new PdfTextExtractor(new NoOpOcrEngine(), Options.Create(new OcrOptions { Enabled = false })), new DocxTextExtractor(), new PptxTextExtractor()),
            new SimpleDocumentChunker(),
            new DocumentEmbeddingService(new StubEmbeddingGenerator(), new FixedDateTimeProvider()),
            new FixedDateTimeProvider());
    }

    private async Task<(Guid DocumentId, Guid JobId)> CreateDocumentWithJobAsync(TestDatabase database, string fileName, FileType fileType, string content)
    {
        var document = new Document
        {
            Name = fileName,
            Status = DocumentStatus.Queued,
        };
        var filePath = Path.Combine(Path.GetTempPath(), $"localmind-ingestion-{Guid.NewGuid():N}-{fileName}");
        await File.WriteAllTextAsync(filePath, content);
        filesToDelete.Add(filePath);

        var documentFile = new DocumentFile
        {
            DocumentId = document.Id,
            FileName = fileName,
            FileType = fileType,
            LocalPath = filePath,
            SizeBytes = content.Length,
        };
        var job = new IngestionJob
        {
            DocumentId = document.Id,
            Status = IngestionJobStatus.Queued,
        };

        database.Context.Documents.Add(document);
        database.Context.DocumentFiles.Add(documentFile);
        database.Context.IngestionJobs.Add(job);
        await database.Context.SaveChangesAsync();
        return (document.Id, job.Id);
    }

    private async Task<(Guid DocumentId, Guid JobId)> CreateDocumentWithJobAsync(TestDatabase database, string fileName, FileType fileType, byte[] content)
    {
        var document = new Document
        {
            Name = fileName,
            Status = DocumentStatus.Queued,
        };
        var filePath = Path.Combine(Path.GetTempPath(), $"localmind-ingestion-{Guid.NewGuid():N}-{fileName}");
        await File.WriteAllBytesAsync(filePath, content);
        filesToDelete.Add(filePath);

        var documentFile = new DocumentFile
        {
            DocumentId = document.Id,
            FileName = fileName,
            FileType = fileType,
            LocalPath = filePath,
            SizeBytes = content.Length,
        };
        var job = new IngestionJob
        {
            DocumentId = document.Id,
            Status = IngestionJobStatus.Queued,
        };

        database.Context.Documents.Add(document);
        database.Context.DocumentFiles.Add(documentFile);
        database.Context.IngestionJobs.Add(job);
        await database.Context.SaveChangesAsync();
        return (document.Id, job.Id);
    }

    private static byte[] CreatePdfBytes(string text)
    {
        var escapedText = text.Replace(@"\", @"\\", StringComparison.Ordinal)
            .Replace("(", @"\(", StringComparison.Ordinal)
            .Replace(")", @"\)", StringComparison.Ordinal);
        var content = $"BT /F1 12 Tf 72 720 Td ({escapedText}) Tj ET";
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}\nendstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
        };
        var builder = new StringBuilder("%PDF-1.4\n");
        var offsets = new List<int> { 0 };
        for (var index = 0; index < objects.Length; index++)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(builder.ToString()));
            builder.Append(CultureInfo.InvariantCulture, $"{index + 1} 0 obj\n{objects[index]}\nendobj\n");
        }

        var xrefOffset = Encoding.ASCII.GetByteCount(builder.ToString());
        builder.Append(CultureInfo.InvariantCulture, $"xref\n0 {objects.Length + 1}\n");
        builder.Append("0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
        {
            builder.Append(CultureInfo.InvariantCulture, $"{offset:D10} 00000 n \n");
        }

        builder.Append(CultureInfo.InvariantCulture, $"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF");
        return Encoding.ASCII.GetBytes(builder.ToString());
    }

    private static byte[] CreateDocxBytes(string text)
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
        {
            var mainDocumentPart = document.AddMainDocumentPart();
            mainDocumentPart.Document = new W.Document(new W.Body(new W.Paragraph(new W.Run(new W.Text(text)))));
            mainDocumentPart.Document.Save();
        }

        return stream.ToArray();
    }

    private static byte[] CreatePptxBytes(string text)
    {
        using var stream = new MemoryStream();
        using (var document = PresentationDocument.Create(stream, PresentationDocumentType.Presentation, true))
        {
            var presentationPart = document.AddPresentationPart();
            presentationPart.Presentation = new P.Presentation();
            var slidePart = presentationPart.AddNewPart<SlidePart>("rId1");
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

    private sealed class FixedDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 5, 13, 12, 0, 0, TimeSpan.Zero);
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection connection;

        private TestDatabase(SqliteConnection connection, AppDbContext context)
        {
            this.connection = connection;
            Context = context;
        }

        public AppDbContext Context { get; }

        public static async Task<TestDatabase> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;
            var context = new AppDbContext(options);
            await context.Database.EnsureCreatedAsync();
            return new TestDatabase(connection, context);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
    private sealed class NoOpOcrEngine : IOcrEngine
    {
        public Task<OcrTextResult> ExtractAsync(
            string imagePath,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new OcrTextResult(
                    Text: string.Empty,
                    DetectedLanguage: null,
                    DetectedScript: null,
                    Confidence: null));
        }
    }
}
