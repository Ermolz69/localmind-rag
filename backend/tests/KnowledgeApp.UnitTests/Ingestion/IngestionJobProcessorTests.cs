using System.Globalization;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Abstractions.Ingestion;
using KnowledgeApp.Application.Ingestion.IncrementalIndexing;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Persistence;
using KnowledgeApp.Infrastructure.Services;
using KnowledgeApp.Infrastructure.Options;
using KnowledgeApp.UnitTests.TestSupport.Builders;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using A = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace KnowledgeApp.UnitTests.Ingestion;

public sealed class IngestionJobProcessorTests
{
    [Fact]
    public async Task ProcessAsync_Should_Index_Text_Document_And_Complete_Job()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        await using DocumentIngestionTestData testData = await DocumentIngestionTestData.CreateAsync(database, "notes.txt", FileType.PlainText, "First paragraph.\n\nSecond paragraph.");
        IngestionJobProcessor processor = CreateProcessor(database);

        await processor.ProcessAsync(testData.JobId);

        DocumentChunk[] chunks = await database.Context.DocumentChunks
            .Where(x => x.DocumentId == testData.DocumentId)
            .OrderBy(x => x.Index)
            .ToArrayAsync();
        Document storedDocument = await database.Context.Documents.SingleAsync(x => x.Id == testData.DocumentId);
        IngestionJob job = await database.Context.IngestionJobs.SingleAsync(x => x.Id == testData.JobId);

        Assert.Single(chunks);
        Assert.Equal("First paragraph.\n\nSecond paragraph.", chunks[0].Text);
        Assert.Equal(DocumentStatus.Indexed, storedDocument.Status);
        Assert.Equal(IngestionJobStatus.Indexed, job.Status);
        Assert.Equal(100, job.ProgressPercent);
        Assert.Equal("Indexed", job.CurrentStep);
        Assert.Null(job.ErrorCode);
        Assert.Null(job.ErrorMessage);
        Assert.NotNull(job.ProcessedAt);

        DocumentEmbedding embedding = await database.Context.DocumentEmbeddings.SingleAsync(x => x.DocumentChunkId == chunks[0].Id);
        Assert.Equal("BGE-M3", embedding.ModelName);
        Assert.Equal(32, embedding.Dimension);
        Assert.Equal(32 * sizeof(float), embedding.Embedding.Length);
    }

    [Fact]
    public async Task ProcessAsync_Should_Remove_Existing_Chunks_And_Embeddings_On_Reindex()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        await using DocumentIngestionTestData testData = await DocumentIngestionTestData.CreateAsync(database, "notes.md", FileType.Markdown, "New content.");
        DocumentChunk oldChunk = new DocumentChunk
        {
            DocumentId = testData.DocumentId,
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
        IngestionJobProcessor processor = CreateProcessor(database);

        await processor.ProcessAsync(testData.JobId);

        DocumentChunk[] chunks = await database.Context.DocumentChunks
            .Where(x => x.DocumentId == testData.DocumentId)
            .ToArrayAsync();
        DocumentEmbedding[] embeddings = await database.Context.DocumentEmbeddings.ToArrayAsync();

        Assert.Single(chunks);
        Assert.Equal("New content.", chunks[0].Text);
        Assert.NotEqual(oldChunk.Id, chunks[0].Id);
        DocumentEmbedding embedding = Assert.Single(embeddings);
        Assert.Equal(chunks[0].Id, embedding.DocumentChunkId);
        Assert.Equal(32, embedding.Dimension);
    }

    [Fact]
    public async Task ProcessAsync_Should_Index_Pdf_Document_And_Complete_Job()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        await using DocumentIngestionTestData testData = await DocumentIngestionTestData.CreateAsync(database, "paper.pdf", FileType.Pdf, CreatePdfBytes("First PDF paragraph."));
        IngestionJobProcessor processor = CreateProcessor(database);

        await processor.ProcessAsync(testData.JobId);

        DocumentChunk[] chunks = await database.Context.DocumentChunks
            .Where(x => x.DocumentId == testData.DocumentId)
            .OrderBy(x => x.Index)
            .ToArrayAsync();
        Document storedDocument = await database.Context.Documents.SingleAsync(x => x.Id == testData.DocumentId);
        IngestionJob job = await database.Context.IngestionJobs.SingleAsync(x => x.Id == testData.JobId);

        Assert.Single(chunks);
        Assert.Contains("First PDF paragraph.", chunks[0].Text, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, chunks[0].PageNumber);
        Assert.Equal(DocumentStatus.Indexed, storedDocument.Status);
        Assert.Equal(IngestionJobStatus.Indexed, job.Status);
        Assert.Null(job.ErrorMessage);
    }

    [Fact]
    public async Task ProcessAsync_Should_Index_Docx_Document_And_Complete_Job()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        await using DocumentIngestionTestData testData = await DocumentIngestionTestData.CreateAsync(database, "document.docx", FileType.Docx, CreateDocxBytes("First DOCX paragraph."));
        IngestionJobProcessor processor = CreateProcessor(database);

        await processor.ProcessAsync(testData.JobId);

        DocumentChunk chunk = await database.Context.DocumentChunks.SingleAsync(x => x.DocumentId == testData.DocumentId);
        Document storedDocument = await database.Context.Documents.SingleAsync(x => x.Id == testData.DocumentId);
        IngestionJob job = await database.Context.IngestionJobs.SingleAsync(x => x.Id == testData.JobId);

        Assert.Equal("First DOCX paragraph.", chunk.Text);
        Assert.Null(chunk.PageNumber);
        Assert.Equal(DocumentStatus.Indexed, storedDocument.Status);
        Assert.Equal(IngestionJobStatus.Indexed, job.Status);
        Assert.Null(job.ErrorMessage);
    }

    [Fact]
    public async Task ProcessAsync_Should_Index_Pptx_Document_And_Complete_Job()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        await using DocumentIngestionTestData testData = await DocumentIngestionTestData.CreateAsync(database, "slides.pptx", FileType.Pptx, CreatePptxBytes("First PPTX paragraph."));
        IngestionJobProcessor processor = CreateProcessor(database);

        await processor.ProcessAsync(testData.JobId);

        DocumentChunk chunk = await database.Context.DocumentChunks.SingleAsync(x => x.DocumentId == testData.DocumentId);
        Document storedDocument = await database.Context.Documents.SingleAsync(x => x.Id == testData.DocumentId);
        IngestionJob job = await database.Context.IngestionJobs.SingleAsync(x => x.Id == testData.JobId);

        Assert.Equal("First PPTX paragraph.", chunk.Text);
        Assert.Equal(1, chunk.PageNumber);
        Assert.Equal(DocumentStatus.Indexed, storedDocument.Status);
        Assert.Equal(IngestionJobStatus.Indexed, job.Status);
        Assert.Null(job.ErrorMessage);
    }

    [Fact]
    public async Task ProcessAsync_Should_Fail_Corrupt_Pdf_Document()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        await using DocumentIngestionTestData testData = await DocumentIngestionTestData.CreateAsync(database, "paper.pdf", FileType.Pdf, Encoding.UTF8.GetBytes("%PDF skeleton"));
        IngestionJobProcessor processor = CreateProcessor(database);

        await processor.ProcessAsync(testData.JobId);

        Document storedDocument = await database.Context.Documents.SingleAsync(x => x.Id == testData.DocumentId);
        IngestionJob job = await database.Context.IngestionJobs.SingleAsync(x => x.Id == testData.JobId);

        Assert.Equal(DocumentStatus.Failed, storedDocument.Status);
        Assert.Equal(IngestionJobStatus.Failed, job.Status);
        Assert.Equal("INGESTION_JOB_FAILED", job.ErrorCode);
        Assert.Contains("PDF", job.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcessAsync_Should_Fail_Corrupt_Docx_Document()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        await using DocumentIngestionTestData testData = await DocumentIngestionTestData.CreateAsync(database, "document.docx", FileType.Docx, Encoding.UTF8.GetBytes("not a docx"));
        IngestionJobProcessor processor = CreateProcessor(database);

        await processor.ProcessAsync(testData.JobId);

        Document storedDocument = await database.Context.Documents.SingleAsync(x => x.Id == testData.DocumentId);
        IngestionJob job = await database.Context.IngestionJobs.SingleAsync(x => x.Id == testData.JobId);

        Assert.Equal(DocumentStatus.Failed, storedDocument.Status);
        Assert.Equal(IngestionJobStatus.Failed, job.Status);
        Assert.Equal("INGESTION_JOB_FAILED", job.ErrorCode);
        Assert.NotNull(job.ErrorMessage);
    }

    [Fact]
    public async Task ProcessAsync_Should_Fail_Corrupt_Pptx_Document()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        await using DocumentIngestionTestData testData = await DocumentIngestionTestData.CreateAsync(database, "slides.pptx", FileType.Pptx, Encoding.UTF8.GetBytes("not a pptx"));
        IngestionJobProcessor processor = CreateProcessor(database);

        await processor.ProcessAsync(testData.JobId);

        Document storedDocument = await database.Context.Documents.SingleAsync(x => x.Id == testData.DocumentId);
        IngestionJob job = await database.Context.IngestionJobs.SingleAsync(x => x.Id == testData.JobId);

        Assert.Equal(DocumentStatus.Failed, storedDocument.Status);
        Assert.Equal(IngestionJobStatus.Failed, job.Status);
        Assert.Equal("INGESTION_JOB_FAILED", job.ErrorCode);
        Assert.NotNull(job.ErrorMessage);
    }

    [Fact]
    public async Task ProcessAsync_Should_Skip_When_Job_Is_Missing()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        IngestionJobProcessor processor = CreateProcessor(database);

        await processor.ProcessAsync(Guid.NewGuid());
    }

    [Fact]
    public async Task ProcessAsync_Should_Not_Process_Already_Processing_Job()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        await using DocumentIngestionTestData testData = await DocumentIngestionTestData.CreateAsync(database, "running.txt", FileType.PlainText, "Running content.");
        IngestionJob job = await database.Context.IngestionJobs.SingleAsync(x => x.Id == testData.JobId);
        job.Status = IngestionJobStatus.Processing;
        await database.Context.SaveChangesAsync();
        IngestionJobProcessor processor = CreateProcessor(database);

        await processor.ProcessAsync(testData.JobId);

        DocumentChunk[] chunks = await database.Context.DocumentChunks
            .Where(x => x.DocumentId == testData.DocumentId)
            .ToArrayAsync();
        IngestionJob storedJob = await database.Context.IngestionJobs.SingleAsync(x => x.Id == testData.JobId);

        Assert.Empty(chunks);
        Assert.Equal(IngestionJobStatus.Processing, storedJob.Status);
    }

    [Fact]
    public async Task ProcessAsync_Should_Not_Process_Cancelled_Job()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        await using DocumentIngestionTestData testData = await DocumentIngestionTestData.CreateAsync(database, "cancelled.txt", FileType.PlainText, "Cancelled content.");
        IngestionJob job = await database.Context.IngestionJobs.SingleAsync(x => x.Id == testData.JobId);
        job.Status = IngestionJobStatus.Cancelled;
        job.CurrentStep = "Cancelled";
        await database.Context.SaveChangesAsync();
        IngestionJobProcessor processor = CreateProcessor(database);

        await processor.ProcessAsync(testData.JobId);

        DocumentChunk[] chunks = await database.Context.DocumentChunks
            .Where(x => x.DocumentId == testData.DocumentId)
            .ToArrayAsync();
        IngestionJob storedJob = await database.Context.IngestionJobs.SingleAsync(x => x.Id == testData.JobId);

        Assert.Empty(chunks);
        Assert.Equal(IngestionJobStatus.Cancelled, storedJob.Status);
    }

    private static IngestionJobProcessor CreateProcessor(ApplicationTestDatabase database)
    {
        RawTextExtractor rawExtractor = new RawTextExtractor();

        return new IngestionJobProcessor(
            database.Context,
            new IngestionJobRepository(database.Context),
            new DocumentTextExtractorFactory(
                rawExtractor,
                new HtmlTextExtractor(),
                new PdfTextExtractor(
                    new NoOpOcrEngine(),
                    Options.Create(new OcrOptions { Enabled = false })),
                new DocxTextExtractor(),
                new PptxTextExtractor()),
            new FakeDocumentChunker(),
            new DocumentEmbeddingService(
                new StubEmbeddingGenerator(),
                new FixedDateTimeProvider()),
            new Sha256ContentHashService(),
            new IncrementalChunkPlanner(),
            new FixedDateTimeProvider());
    }

    private static byte[] CreatePdfBytes(string text)
    {
        string escapedText = text.Replace(@"\", @"\\", StringComparison.Ordinal)
            .Replace("(", @"\(", StringComparison.Ordinal)
            .Replace(")", @"\)", StringComparison.Ordinal);
        string content = $"BT /F1 12 Tf 72 720 Td ({escapedText}) Tj ET";
        string[] objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}\nendstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
        };

        StringBuilder builder = new StringBuilder("%PDF-1.4\n");
        List<int> offsets = new List<int> { 0 };
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
        using MemoryStream stream = new MemoryStream();
        using (WordprocessingDocument document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
        {
            MainDocumentPart mainDocumentPart = document.AddMainDocumentPart();
            mainDocumentPart.Document = new W.Document(new W.Body(new W.Paragraph(new W.Run(new W.Text(text)))));
            mainDocumentPart.Document.Save();
        }

        return stream.ToArray();
    }

    private static byte[] CreatePptxBytes(string text)
    {
        using MemoryStream stream = new MemoryStream();
        using (PresentationDocument document = PresentationDocument.Create(stream, PresentationDocumentType.Presentation, true))
        {
            PresentationPart presentationPart = document.AddPresentationPart();
            presentationPart.Presentation = new P.Presentation();
            SlidePart slidePart = presentationPart.AddNewPart<SlidePart>("rId1");
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

    private sealed class FakeDocumentChunker : IDocumentChunker
    {
        public IReadOnlyList<DocumentChunkText> SplitDetailed(string text)
        {
            return new[]
            {
                new DocumentChunkText(
                    Text: text,
                    CoreText: text,
                    HasOverlap: false,
                    HeadingPath: null,
                    SectionTitle: null,
                    ChunkType: "unknown",
                    SourceStartOffset: null,
                    SourceEndOffset: null,
                    TokenCount: 10,
                    TokenizerId: "test-tokenizer",
                    ChunkingAlgorithmId: "test-alg",
                    ChunkIdentityHash: text,
                    EmbeddingTextHash: text)
            };
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
