using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Documents;
using KnowledgeApp.Contracts.Rag;
using KnowledgeApp.Contracts.Runtime;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Options;
using KnowledgeApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UglyToad.PdfPig;
using A = DocumentFormat.OpenXml.Drawing;
using PresentationSlideId = DocumentFormat.OpenXml.Presentation.SlideId;
using SlideText = DocumentFormat.OpenXml.Drawing.Text;
using WordParagraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;
using WordText = DocumentFormat.OpenXml.Wordprocessing.Text;

namespace KnowledgeApp.Infrastructure.Services;

public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

public sealed class UnitOfWork(AppDbContext dbContext) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => dbContext.SaveChangesAsync(cancellationToken);
}

public sealed class LocalFileStorageService(IAppPathProvider paths) : IFileStorageService
{
    public async Task<StoredFileDto> SaveAsync(Stream content, Guid documentId, string fileName, CancellationToken cancellationToken = default)
    {
        var safeFileName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            throw new ArgumentException("Document file name is required.", nameof(fileName));
        }

        var documentDirectory = Path.Combine(paths.FilesDirectory, documentId.ToString());
        Directory.CreateDirectory(documentDirectory);
        var localPath = Path.Combine(documentDirectory, safeFileName);
        long sizeBytes;
        await using (var output = File.Create(localPath))
        {
            await content.CopyToAsync(output, cancellationToken);
            await output.FlushAsync(cancellationToken);
            sizeBytes = output.Length;
        }

        await using var input = File.OpenRead(localPath);
        var hash = Convert.ToHexString(await SHA256.HashDataAsync(input, cancellationToken));
        return new StoredFileDto(safeFileName, localPath, sizeBytes, hash);
    }
}

public sealed class IngestionQueue(AppDbContext dbContext) : IIngestionQueue
{
    public async Task EnqueueAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        dbContext.IngestionJobs.Add(new IngestionJob { DocumentId = documentId, Status = IngestionJobStatus.Queued });
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

public sealed class IngestionJobProcessor(
    AppDbContext dbContext,
    IDocumentTextExtractorFactory extractorFactory,
    IDocumentChunker chunker,
    IDocumentEmbeddingService documentEmbeddingService,
    IDateTimeProvider dateTimeProvider) : IIngestionJobProcessor
{
    public async Task ProcessAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var job = await dbContext.IngestionJobs.FindAsync([jobId], cancellationToken);
        if (job is null)
        {
            throw new InvalidOperationException("Ingestion job was not found.");
        }

        if (job.Status != IngestionJobStatus.Queued)
        {
            return;
        }

        var document = await dbContext.Documents.FindAsync([job.DocumentId], cancellationToken);
        if (document is null)
        {
            job.Status = IngestionJobStatus.Failed;
            job.LastError = "Document was not found.";
            job.ProcessedAt = dateTimeProvider.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        job.Status = IngestionJobStatus.Running;
        document.Status = DocumentStatus.Processing;
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var documentFile = await dbContext.DocumentFiles
                .Where(x => x.DocumentId == document.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (documentFile is null)
            {
                throw new InvalidOperationException("Document file was not found.");
            }

            var extension = Path.GetExtension(documentFile.FileName);
            var extractor = extractorFactory.GetExtractor(documentFile.FileType, extension, null);
            var extraction = await extractor.ExtractAsync(documentFile.LocalPath, cancellationToken);
            var existingChunks = await dbContext.DocumentChunks
                .Where(x => x.DocumentId == document.Id)
                .ToArrayAsync(cancellationToken);
            var existingChunkIds = existingChunks.Select(x => x.Id).ToArray();
            var existingEmbeddings = await dbContext.DocumentEmbeddings
                .Where(x => existingChunkIds.Contains(x.DocumentChunkId))
                .ToArrayAsync(cancellationToken);

            dbContext.DocumentEmbeddings.RemoveRange(existingEmbeddings);
            dbContext.DocumentChunks.RemoveRange(existingChunks);
            var newChunks = new List<DocumentChunk>();
            foreach (var segment in extraction.Segments)
            {
                foreach (var chunkText in chunker.Split(segment.Text))
                {
                    newChunks.Add(new DocumentChunk
                    {
                        CreatedAt = dateTimeProvider.UtcNow,
                        DocumentId = document.Id,
                        Index = newChunks.Count,
                        PageNumber = segment.PageNumber,
                        Text = chunkText,
                    });
                }
            }

            if (newChunks.Count == 0)
            {
                throw new InvalidOperationException("No extractable text was found in the document.");
            }

            dbContext.DocumentChunks.AddRange(newChunks);
            var newEmbeddings = await documentEmbeddingService.GenerateAsync(newChunks, cancellationToken);
            dbContext.DocumentEmbeddings.AddRange(newEmbeddings);

            job.Status = IngestionJobStatus.Completed;
            job.LastError = null;
            job.ProcessedAt = dateTimeProvider.UtcNow;
            document.Status = DocumentStatus.Indexed;
        }
        catch (Exception exception)
        {
            job.Status = IngestionJobStatus.Failed;
            job.LastError = exception.Message;
            job.ProcessedAt = dateTimeProvider.UtcNow;
            document.Status = DocumentStatus.Failed;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

public sealed class SimpleDocumentChunker : IDocumentChunker
{
    private const int TargetChunkSize = 1200;

    public IReadOnlyList<string> Split(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var normalizedText = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        var paragraphs = normalizedText
            .Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(paragraph => Regex.Replace(paragraph, @"\s+", " ").Trim())
            .Where(paragraph => paragraph.Length > 0);

        var chunks = new List<string>();
        var current = new StringBuilder();
        foreach (var paragraph in paragraphs)
        {
            if (paragraph.Length > TargetChunkSize)
            {
                FlushCurrentChunk(chunks, current);
                chunks.AddRange(SplitLongParagraph(paragraph));
                continue;
            }

            if (current.Length > 0 && current.Length + 2 + paragraph.Length > TargetChunkSize)
            {
                FlushCurrentChunk(chunks, current);
            }

            if (current.Length > 0)
            {
                current.Append("\n\n");
            }

            current.Append(paragraph);
        }

        FlushCurrentChunk(chunks, current);
        return chunks;
    }

    private static IEnumerable<string> SplitLongParagraph(string paragraph)
    {
        for (var index = 0; index < paragraph.Length; index += TargetChunkSize)
        {
            yield return paragraph.Substring(index, Math.Min(TargetChunkSize, paragraph.Length - index));
        }
    }

    private static void FlushCurrentChunk(ICollection<string> chunks, StringBuilder current)
    {
        if (current.Length == 0)
        {
            return;
        }

        chunks.Add(current.ToString());
        current.Clear();
    }
}

public sealed class RawTextExtractor : IDocumentTextExtractor
{
    public async Task<DocumentTextExtractionResult> ExtractAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var text = await File.ReadAllTextAsync(filePath, cancellationToken);
        return ExtractedText.FromSingle(text);
    }
}

public sealed class PdfTextExtractor : IDocumentTextExtractor
{
    public Task<DocumentTextExtractionResult> ExtractAsync(string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            FileSignatureValidator.EnsurePdf(filePath);
            using var document = PdfDocument.Open(filePath);
            var pages = new List<DocumentTextSegment>();
            foreach (var page in document.GetPages())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!string.IsNullOrWhiteSpace(page.Text))
                {
                    pages.Add(new DocumentTextSegment(page.Text.Trim(), page.Number, $"Page {page.Number}", "PdfPage"));
                }
            }

            return Task.FromResult(ExtractedText.FromSegments(pages));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"Failed to extract text from PDF document: {exception.Message}", exception);
        }
    }
}

public sealed class DocxTextExtractor : IDocumentTextExtractor
{
    public Task<DocumentTextExtractionResult> ExtractAsync(string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            FileSignatureValidator.EnsureZipPackage(filePath, "DOCX");
            using var document = WordprocessingDocument.Open(filePath, false);
            var body = document.MainDocumentPart?.Document?.Body;
            if (body is null)
            {
                throw new InvalidOperationException("DOCX document body was not found.");
            }

            var paragraphs = body
                .Descendants<WordParagraph>()
                .Select(ExtractWordParagraphText)
                .Where(paragraph => paragraph.Length > 0);

            return Task.FromResult(ExtractedText.FromSingle(string.Join("\n\n", paragraphs), "Document", "DocxDocument"));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"Failed to extract text from DOCX document: {exception.Message}", exception);
        }
    }

    private static string ExtractWordParagraphText(WordParagraph paragraph)
    {
        var text = string.Concat(paragraph.Descendants<WordText>().Select(text => text.Text)).Trim();
        var style = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
        return string.IsNullOrWhiteSpace(style) ? text : $"{style}: {text}";
    }
}

public sealed class PptxTextExtractor : IDocumentTextExtractor
{
    public Task<DocumentTextExtractionResult> ExtractAsync(string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            FileSignatureValidator.EnsureZipPackage(filePath, "PPTX");
            using var document = PresentationDocument.Open(filePath, false);
            var presentationPart = document.PresentationPart;
            var slideIdList = presentationPart?.Presentation?.SlideIdList;
            if (presentationPart is null || slideIdList is null)
            {
                throw new InvalidOperationException("PPTX slide list was not found.");
            }

            var slides = new List<DocumentTextSegment>();
            var slideNumber = 1;
            foreach (var slideId in slideIdList.Elements<PresentationSlideId>())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var relationshipId = slideId.RelationshipId?.Value;
                if (string.IsNullOrWhiteSpace(relationshipId))
                {
                    continue;
                }

                var slidePart = (SlidePart)presentationPart.GetPartById(relationshipId);
                var slideText = ExtractSlideText(slidePart).Trim();
                if (slideText.Length > 0)
                {
                    slides.Add(new DocumentTextSegment(slideText, slideNumber, $"Slide {slideNumber}", "PptxSlide"));
                }

                slideNumber++;
            }

            return Task.FromResult(ExtractedText.FromSegments(slides));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"Failed to extract text from PPTX document: {exception.Message}", exception);
        }
    }

    private static string ExtractSlideText(SlidePart slidePart)
    {
        var textBlocks = new List<string>();
        if (slidePart.Slide is not null)
        {
            textBlocks.AddRange(slidePart.Slide
                .Descendants<A.Paragraph>()
                .Select(paragraph => string.Join(string.Empty, paragraph.Descendants<SlideText>().Select(text => text.Text)).Trim())
                .Where(text => text.Length > 0));
        }

        if (slidePart.NotesSlidePart?.NotesSlide is not null)
        {
            textBlocks.AddRange(slidePart.NotesSlidePart.NotesSlide
                .Descendants<A.Paragraph>()
                .Select(paragraph => string.Join(string.Empty, paragraph.Descendants<SlideText>().Select(text => text.Text)).Trim())
                .Where(text => text.Length > 0));
        }

        return string.Join("\n\n", textBlocks);
    }
}

public sealed partial class HtmlTextExtractor : IDocumentTextExtractor
{
    public async Task<DocumentTextExtractionResult> ExtractAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var html = await File.ReadAllTextAsync(filePath, cancellationToken);
        var withoutScripts = ScriptOrStyleRegex().Replace(html, " ");
        var withoutTags = HtmlTagRegex().Replace(withoutScripts, " ");
        return ExtractedText.FromSingle(WebUtility.HtmlDecode(withoutTags), "Document", "HtmlDocument");
    }

    [GeneratedRegex("<(script|style)[^>]*>.*?</\\1>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ScriptOrStyleRegex();

    [GeneratedRegex("<[^>]+>", RegexOptions.IgnoreCase)]
    private static partial Regex HtmlTagRegex();
}

public sealed class UnsupportedDocumentTextExtractor(FileType fileType) : IDocumentTextExtractor
{
    public Task<DocumentTextExtractionResult> ExtractAsync(string filePath, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException($"Document text extraction is not supported for {fileType} files yet.");
}

public sealed class DocumentTextExtractorFactory(
    RawTextExtractor rawTextExtractor,
    HtmlTextExtractor htmlTextExtractor,
    PdfTextExtractor pdfTextExtractor,
    DocxTextExtractor docxTextExtractor,
    PptxTextExtractor pptxTextExtractor) : IDocumentTextExtractorFactory
{
    public IDocumentTextExtractor GetExtractor(FileType fileType, string extension, string? mimeType)
    {
        var normalizedExtension = extension.ToLowerInvariant();
        return fileType switch
        {
            FileType.Pdf => pdfTextExtractor,
            FileType.Docx => docxTextExtractor,
            FileType.Pptx => pptxTextExtractor,
            FileType.PlainText => rawTextExtractor,
            FileType.Markdown => rawTextExtractor,
            FileType.Html => htmlTextExtractor,
            FileType.Unknown when normalizedExtension is ".pdf" => pdfTextExtractor,
            FileType.Unknown when normalizedExtension is ".docx" => docxTextExtractor,
            FileType.Unknown when normalizedExtension is ".pptx" => pptxTextExtractor,
            FileType.Unknown when normalizedExtension is ".txt" => rawTextExtractor,
            FileType.Unknown when normalizedExtension is ".md" or ".markdown" => rawTextExtractor,
            FileType.Unknown when normalizedExtension is ".html" or ".htm" => htmlTextExtractor,
            _ => new UnsupportedDocumentTextExtractor(fileType),
        };
    }
}

internal static class ExtractedText
{
    public static DocumentTextExtractionResult FromSingle(string text, string? sectionTitle = null, string sourceKind = "Document")
    {
        return FromSegments([new DocumentTextSegment(text, null, sectionTitle, sourceKind)]);
    }

    public static DocumentTextExtractionResult FromSegments(IEnumerable<DocumentTextSegment> segments)
    {
        var cleanSegments = segments
            .Select(segment => segment with { Text = segment.Text.Trim() })
            .Where(segment => segment.Text.Length > 0)
            .ToArray();

        if (cleanSegments.Length == 0)
        {
            throw new InvalidOperationException("No extractable text was found in the document.");
        }

        return new DocumentTextExtractionResult(cleanSegments);
    }
}

internal static class FileSignatureValidator
{
    public static void EnsurePdf(string filePath)
    {
        Span<byte> signature = stackalloc byte[5];
        using var input = File.OpenRead(filePath);
        if (input.Read(signature) != signature.Length || !signature.SequenceEqual("%PDF-"u8))
        {
            throw new InvalidOperationException("PDF file signature is invalid.");
        }
    }

    public static void EnsureZipPackage(string filePath, string format)
    {
        Span<byte> signature = stackalloc byte[4];
        using var input = File.OpenRead(filePath);
        if (input.Read(signature) != signature.Length
            || signature[0] != (byte)'P'
            || signature[1] != (byte)'K')
        {
            throw new InvalidOperationException($"{format} file signature is invalid.");
        }
    }
}

public sealed class StubEmbeddingGenerator : IEmbeddingGenerator
{
    private const string DefaultModelName = "BGE-M3";
    private readonly string modelName;

    public StubEmbeddingGenerator() : this(DefaultModelName)
    {
    }

    public StubEmbeddingGenerator(IOptions<AiOptions> options) : this(options.Value.EmbeddingModel)
    {
    }

    private StubEmbeddingGenerator(string modelName)
    {
        this.modelName = string.IsNullOrWhiteSpace(modelName) ? DefaultModelName : modelName;
    }

    public string ModelName => modelName;

    public Task<float[]> GenerateAsync(string text, CancellationToken cancellationToken = default)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Task.FromResult(bytes.Select(x => (float)x / byte.MaxValue).ToArray());
    }
}

public sealed class DocumentEmbeddingService(
    IEmbeddingGenerator embeddingGenerator,
    IDateTimeProvider dateTimeProvider) : IDocumentEmbeddingService
{
    public async Task<IReadOnlyList<DocumentEmbedding>> GenerateAsync(
        IReadOnlyList<DocumentChunk> chunks,
        CancellationToken cancellationToken = default)
    {
        var embeddings = new List<DocumentEmbedding>(chunks.Count);
        foreach (var chunk in chunks)
        {
            var vector = await embeddingGenerator.GenerateAsync(chunk.Text, cancellationToken);
            embeddings.Add(new DocumentEmbedding
            {
                CreatedAt = dateTimeProvider.UtcNow,
                DocumentChunkId = chunk.Id,
                ModelName = embeddingGenerator.ModelName,
                Dimension = vector.Length,
                Embedding = EmbeddingVectorSerializer.ToBytes(vector),
            });
        }

        return embeddings;
    }
}

internal static class EmbeddingVectorSerializer
{
    public static byte[] ToBytes(float[] vector)
    {
        var bytes = new byte[vector.Length * sizeof(float)];
        Buffer.BlockCopy(vector, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    public static float[] FromBytes(byte[] bytes)
    {
        if (bytes.Length % sizeof(float) != 0)
        {
            throw new InvalidOperationException("Embedding byte length is not a multiple of float size.");
        }

        var vector = new float[bytes.Length / sizeof(float)];
        Buffer.BlockCopy(bytes, 0, vector, 0, bytes.Length);
        return vector;
    }
}

public sealed class StubChatModelClient : IChatModelClient
{
    public Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken = default) => Task.FromResult("Local AI runtime is not configured yet.");
}

public sealed class ExactVectorSearchService(AppDbContext dbContext) : IVectorSearchService, IVectorIndex
{
    public Task UpsertAsync(Guid chunkId, float[] vector, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public async Task<IReadOnlyList<RagSourceDto>> SearchAsync(float[] queryVector, VectorSearchOptions options, CancellationToken cancellationToken = default)
    {
        if (queryVector.Length == 0 || options.Limit <= 0)
        {
            return [];
        }

        var rowsQuery =
            from embedding in dbContext.DocumentEmbeddings
            join chunk in dbContext.DocumentChunks on embedding.DocumentChunkId equals chunk.Id
            join document in dbContext.Documents on chunk.DocumentId equals document.Id
            select new
            {
                DocumentId = document.Id,
                DocumentName = document.Name,
                DocumentBucketId = document.BucketId,
                ChunkId = chunk.Id,
                chunk.PageNumber,
                chunk.Text,
                embedding.Dimension,
                embedding.Embedding,
            };

        if (options.DocumentId is { } documentId)
        {
            rowsQuery = rowsQuery.Where(x => x.DocumentId == documentId);
        }

        if (options.BucketId is { } bucketId)
        {
            rowsQuery = rowsQuery.Where(x => x.DocumentBucketId == bucketId);
        }

        var rows = await rowsQuery.ToArrayAsync(cancellationToken);

        return rows
            .Where(x => x.Dimension == queryVector.Length)
            .Select(x =>
            {
                var chunkVector = EmbeddingVectorSerializer.FromBytes(x.Embedding);
                var score = CosineSimilarity(queryVector, chunkVector);
                return new RagSourceDto(x.DocumentId, x.DocumentName, x.ChunkId, x.PageNumber, score, x.Text);
            })
            .OrderByDescending(x => x.Score)
            .Take(options.Limit)
            .ToArray();
    }

    private static double CosineSimilarity(float[] left, float[] right)
    {
        if (left.Length == 0 || left.Length != right.Length)
        {
            return 0;
        }

        double dotProduct = 0;
        double leftMagnitude = 0;
        double rightMagnitude = 0;

        for (var i = 0; i < left.Length; i++)
        {
            dotProduct += left[i] * right[i];
            leftMagnitude += left[i] * left[i];
            rightMagnitude += right[i] * right[i];
        }

        if (leftMagnitude == 0 || rightMagnitude == 0)
        {
            return 0;
        }

        return dotProduct / (Math.Sqrt(leftMagnitude) * Math.Sqrt(rightMagnitude));
    }
}

public sealed class RagContextBuilder(IVectorSearchService search, IEmbeddingGenerator embeddings) : IRagContextBuilder
{
    public async Task<IReadOnlyList<RagSourceDto>> BuildAsync(string question, CancellationToken cancellationToken = default)
    {
        var vector = await embeddings.GenerateAsync(question, cancellationToken);
        return await search.SearchAsync(vector, new VectorSearchOptions(), cancellationToken);
    }
}

public sealed class RagAnswerGenerator(IRagContextBuilder contextBuilder, IChatModelClient chatClient) : IRagAnswerGenerator
{
    public async Task<RagAnswerDto> AnswerAsync(Guid conversationId, string question, CancellationToken cancellationToken = default)
    {
        var sources = await contextBuilder.BuildAsync(question, cancellationToken);
        var answer = await chatClient.GenerateAsync(question, cancellationToken);
        return new RagAnswerDto(answer, sources);
    }
}

public sealed class AiRuntimeManager : IAiRuntimeManager, IAiModelRegistry
{
    public Task<RuntimeStatusDto> GetStatusAsync(CancellationToken cancellationToken = default) => Task.FromResult(new RuntimeStatusDto(true, "Missing", false, true));
    public Task<IReadOnlyCollection<string>> ListModelsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<string>>([]);
    public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}

public sealed class SyncService(AppDbContext dbContext) : ISyncService, ISyncQueue, ISyncClient
{
    public async Task<SyncStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var pending = await dbContext.SyncOutbox.CountAsync(x => x.Status == SyncStatus.PendingUpload, cancellationToken);
        return new SyncStatusDto(false, false, pending, "Sync disabled");
    }

    public Task RunAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task PushAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task PullAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task EnqueueAsync(Guid entityId, SyncOperation operation, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

public sealed class NetworkStatusService : INetworkStatusService
{
    public Task<bool> IsOnlineAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);
}

public sealed class CurrentUserService : ICurrentUserService
{
    public Guid? UserId => null;
}

public sealed class AppLockService : IAppLockService
{
    public Task<bool> TryAcquireAsync(string key, CancellationToken cancellationToken = default) => Task.FromResult(true);
}
