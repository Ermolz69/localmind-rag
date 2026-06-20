using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Abstractions.Ingestion;
using KnowledgeApp.Application.Ingestion.IncrementalIndexing;
using KnowledgeApp.Contracts.Documents;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Options;
using KnowledgeApp.Infrastructure.Persistence;
using KnowledgeApp.Infrastructure.Services;
using KnowledgeApp.UnitTests.TestSupport.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.UnitTests.Ingestion;

public sealed class IncrementalIngestionJobProcessorTests
{
    [Fact]
    public async Task ProcessAsync_Should_NotGenerateEmbeddingsAgain_WhenSameDocumentIsReindexedWithoutChanges()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        CountingDocumentEmbeddingService embeddingService = new CountingDocumentEmbeddingService();
        await using DocumentIngestionTestData testData = await DocumentIngestionTestData.CreateAsync(
            database,
            "unchanged.txt",
            FileType.PlainText,
            "Alpha||Beta||Gamma");
        IngestionJobProcessor processor = CreateProcessor(database, embeddingService);

        await processor.ProcessAsync(testData.JobId);

        DocumentChunk[] firstChunks = await database.Context.DocumentChunks
            .Where(chunk => chunk.DocumentId == testData.DocumentId)
            .OrderBy(chunk => chunk.Index)
            .ToArrayAsync();

        Guid[] firstChunkIds = firstChunks
            .Select(chunk => chunk.Id)
            .ToArray();

        Assert.Equal(3, firstChunks.Length);
        Assert.Equal(3, embeddingService.GeneratedChunkTexts.Count);

        Guid secondJobId = await CreateReindexJobAsync(database, testData.DocumentId);

        await processor.ProcessAsync(secondJobId);

        DocumentChunk[] secondChunks = await database.Context.DocumentChunks
            .Where(chunk => chunk.DocumentId == testData.DocumentId)
            .OrderBy(chunk => chunk.Index)
            .ToArrayAsync();

        Guid[] secondChunkIds = secondChunks
            .Select(chunk => chunk.Id)
            .ToArray();

        DocumentEmbedding[] embeddings = await database.Context.DocumentEmbeddings
            .ToArrayAsync();

        Assert.Equal(firstChunkIds, secondChunkIds);
        Assert.Equal(3, secondChunks.Length);
        Assert.Equal(3, embeddings.Length);
        Assert.Equal(3, embeddingService.GeneratedChunkTexts.Count);
    }

    [Fact]
    public async Task ProcessAsync_Should_GenerateEmbeddingOnlyForChangedChunk_WhenSameDocumentContentChanges()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        CountingDocumentEmbeddingService embeddingService = new CountingDocumentEmbeddingService();
        await using DocumentIngestionTestData testData = await DocumentIngestionTestData.CreateAsync(
            database,
            "changed.txt",
            FileType.PlainText,
            "Alpha||Beta||Gamma");
        IngestionJobProcessor processor = CreateProcessor(database, embeddingService);

        await processor.ProcessAsync(testData.JobId);

        DocumentChunk[] firstChunks = await database.Context.DocumentChunks
            .Where(chunk => chunk.DocumentId == testData.DocumentId)
            .OrderBy(chunk => chunk.Index)
            .ToArrayAsync();

        Guid alphaChunkId = firstChunks[0].Id;
        Guid betaChunkId = firstChunks[1].Id;
        Guid gammaChunkId = firstChunks[2].Id;

        Assert.Equal(3, embeddingService.GeneratedChunkTexts.Count);

        await File.WriteAllTextAsync(testData.FilePath, "Alpha||Beta modified||Gamma");

        Guid secondJobId = await CreateReindexJobAsync(database, testData.DocumentId);

        await processor.ProcessAsync(secondJobId);

        DocumentChunk[] secondChunks = await database.Context.DocumentChunks
            .Where(chunk => chunk.DocumentId == testData.DocumentId)
            .OrderBy(chunk => chunk.Index)
            .ToArrayAsync();

        DocumentEmbedding[] embeddings = await database.Context.DocumentEmbeddings
            .ToArrayAsync();

        Assert.Equal(3, secondChunks.Length);
        Assert.Equal(3, embeddings.Length);

        Assert.Equal(alphaChunkId, secondChunks[0].Id);
        Assert.NotEqual(betaChunkId, secondChunks[1].Id);
        Assert.Equal(gammaChunkId, secondChunks[2].Id);

        Assert.Equal("Alpha", secondChunks[0].Text);
        Assert.Equal("Beta modified", secondChunks[1].Text);
        Assert.Equal("Gamma", secondChunks[2].Text);

        Assert.DoesNotContain(secondChunks, chunk => chunk.Id == betaChunkId);

        Assert.Equal(4, embeddingService.GeneratedChunkTexts.Count);
        Assert.Equal("Beta modified", embeddingService.GeneratedChunkTexts[^1]);
    }

    [Fact]
    public async Task ProcessAsync_Should_RemoveDeletedChunkAndEmbedding_WhenChunkIsRemovedFromDocument()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        CountingDocumentEmbeddingService embeddingService = new CountingDocumentEmbeddingService();
        await using DocumentIngestionTestData testData = await DocumentIngestionTestData.CreateAsync(
            database,
            "deleted.txt",
            FileType.PlainText,
            "Alpha||Beta||Gamma");
        IngestionJobProcessor processor = CreateProcessor(database, embeddingService);

        await processor.ProcessAsync(testData.JobId);

        DocumentChunk[] firstChunks = await database.Context.DocumentChunks
            .Where(chunk => chunk.DocumentId == testData.DocumentId)
            .OrderBy(chunk => chunk.Index)
            .ToArrayAsync();

        Guid alphaChunkId = firstChunks[0].Id;
        Guid betaChunkId = firstChunks[1].Id;
        Guid gammaChunkId = firstChunks[2].Id;

        await File.WriteAllTextAsync(testData.FilePath, "Alpha||Gamma");

        Guid secondJobId = await CreateReindexJobAsync(database, testData.DocumentId);

        await processor.ProcessAsync(secondJobId);

        DocumentChunk[] secondChunks = await database.Context.DocumentChunks
            .Where(chunk => chunk.DocumentId == testData.DocumentId)
            .OrderBy(chunk => chunk.Index)
            .ToArrayAsync();

        DocumentEmbedding[] embeddings = await database.Context.DocumentEmbeddings
            .ToArrayAsync();

        Assert.Equal(2, secondChunks.Length);
        Assert.Equal(2, embeddings.Length);

        Assert.Equal(alphaChunkId, secondChunks[0].Id);
        Assert.Equal(gammaChunkId, secondChunks[1].Id);
        Assert.DoesNotContain(secondChunks, chunk => chunk.Id == betaChunkId);

        Assert.Equal(0, secondChunks[0].Index);
        Assert.Equal(1, secondChunks[1].Index);

        Assert.Equal("Alpha", secondChunks[0].Text);
        Assert.Equal("Gamma", secondChunks[1].Text);

        Assert.DoesNotContain(embeddings, embedding => embedding.DocumentChunkId == betaChunkId);

        Assert.Equal(3, embeddingService.GeneratedChunkTexts.Count);
    }

    [Fact]
    public async Task ProcessAsync_Should_ReuseEmbeddingsAcrossDocuments_WhenSameContentIsUploadedAgain()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        CountingDocumentEmbeddingService embeddingService = new CountingDocumentEmbeddingService();
        await using DocumentIngestionTestData first = await DocumentIngestionTestData.CreateAsync(
            database,
            "first.txt",
            FileType.PlainText,
            "Alpha||Beta||Gamma");
        IngestionJobProcessor processor = CreateProcessor(database, embeddingService);

        await processor.ProcessAsync(first.JobId);

        Assert.Equal(3, embeddingService.GeneratedChunkTexts.Count);

        await using DocumentIngestionTestData second = await DocumentIngestionTestData.CreateAsync(
            database,
            "second.txt",
            FileType.PlainText,
            "Alpha||Beta||Gamma");

        await processor.ProcessAsync(second.JobId);

        DocumentChunk[] secondDocumentChunks = await database.Context.DocumentChunks
            .Where(chunk => chunk.DocumentId == second.DocumentId)
            .OrderBy(chunk => chunk.Index)
            .ToArrayAsync();

        DocumentEmbedding[] secondDocumentEmbeddings = await database.Context.DocumentEmbeddings
            .Where(embedding => secondDocumentChunks.Select(chunk => chunk.Id).Contains(embedding.DocumentChunkId))
            .ToArrayAsync();

        Assert.Equal(3, secondDocumentChunks.Length);
        Assert.Equal(3, secondDocumentEmbeddings.Length);

        Assert.Equal("Alpha", secondDocumentChunks[0].Text);
        Assert.Equal("Beta", secondDocumentChunks[1].Text);
        Assert.Equal("Gamma", secondDocumentChunks[2].Text);

        Assert.Equal(3, embeddingService.GeneratedChunkTexts.Count);

        Assert.All(secondDocumentEmbeddings, embedding =>
        {
            Assert.Equal("BGE-M3", embedding.ModelName);
            Assert.Equal(2, embedding.Dimension);
            Assert.Equal(2 * sizeof(float), embedding.Embedding.Length);
        });
    }

    private static IngestionJobProcessor CreateProcessor(
        ApplicationTestDatabase database,
        CountingDocumentEmbeddingService embeddingService)
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
            new PipeSeparatedChunker(),
            embeddingService,
            new Sha256ContentHashService(),
            new IncrementalChunkPlanner(),
            new FixedDateTimeProvider());
    }

    private static async Task<Guid> CreateReindexJobAsync(ApplicationTestDatabase database, Guid documentId)
    {
        Document document = await database.Context.Documents.SingleAsync(document => document.Id == documentId);

        document.Status = DocumentStatus.Queued;

        IngestionJob job = new IngestionJob
        {
            DocumentId = documentId,
            Status = IngestionJobStatus.Pending
        };

        database.Context.IngestionJobs.Add(job);

        await database.Context.SaveChangesAsync();

        return job.Id;
    }

    private sealed class PipeSeparatedChunker : IDocumentChunker
    {
        public IReadOnlyList<DocumentChunkText> SplitDetailed(string text)
        {
            return text
                .Split("||", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(chunk => !string.IsNullOrWhiteSpace(chunk))
                .Select(chunk => new DocumentChunkText(
                    Text: chunk,
                    CoreText: chunk,
                    HasOverlap: false,
                    HeadingPath: null,
                    SectionTitle: null,
                    ChunkType: "unknown",
                    SourceStartOffset: null,
                    SourceEndOffset: null,
                    TokenCount: 10,
                    TokenizerId: "test-tokenizer",
                    ChunkingAlgorithmId: "test-alg",
                    ChunkIdentityHash: chunk,
                    EmbeddingTextHash: chunk))
                .ToArray();
        }
    }

    private sealed class CountingDocumentEmbeddingService : IDocumentEmbeddingService
    {
        public string ModelName => "BGE-M3";

        public List<string> GeneratedChunkTexts { get; } = [];

        public Task<IReadOnlyList<DocumentEmbedding>> GenerateAsync(
            IReadOnlyList<DocumentChunk> chunks,
            CancellationToken cancellationToken = default)
        {
            List<DocumentEmbedding> embeddings = new List<DocumentEmbedding>(chunks.Count);

            foreach (DocumentChunk chunk in chunks)
            {
                GeneratedChunkTexts.Add(chunk.Text);

                embeddings.Add(new DocumentEmbedding
                {
                    DocumentChunkId = chunk.Id,
                    ModelName = ModelName,
                    Dimension = 2,
                    Embedding = new byte[2 * sizeof(float)]
                });
            }

            return Task.FromResult<IReadOnlyList<DocumentEmbedding>>(embeddings);
        }
    }

    private sealed class NoOpOcrEngine : IOcrEngine
    {
        public Task<OcrTextResult> ExtractAsync(
            string imagePath,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new OcrTextResult(
                Text: string.Empty,
                DetectedLanguage: null,
                DetectedScript: null,
                Confidence: null));
        }
    }
}
