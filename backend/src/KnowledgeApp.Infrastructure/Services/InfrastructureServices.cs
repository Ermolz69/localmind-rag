using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Documents;
using KnowledgeApp.Contracts.Rag;
using KnowledgeApp.Contracts.Runtime;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

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
            var text = await extractor.ExtractAsync(documentFile.LocalPath, cancellationToken);
            var chunkTexts = chunker.Split(text);
            var existingChunks = await dbContext.DocumentChunks
                .Where(x => x.DocumentId == document.Id)
                .ToArrayAsync(cancellationToken);

            dbContext.DocumentChunks.RemoveRange(existingChunks);
            dbContext.DocumentChunks.AddRange(chunkTexts.Select((chunkText, index) => new DocumentChunk
            {
                CreatedAt = dateTimeProvider.UtcNow,
                DocumentId = document.Id,
                Index = index,
                PageNumber = null,
                Text = chunkText,
            }));

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
    public Task<string> ExtractAsync(string filePath, CancellationToken cancellationToken = default) => File.ReadAllTextAsync(filePath, cancellationToken);
}

public sealed partial class HtmlTextExtractor : IDocumentTextExtractor
{
    public async Task<string> ExtractAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var html = await File.ReadAllTextAsync(filePath, cancellationToken);
        var withoutScripts = ScriptOrStyleRegex().Replace(html, " ");
        var withoutTags = HtmlTagRegex().Replace(withoutScripts, " ");
        return WebUtility.HtmlDecode(withoutTags);
    }

    [GeneratedRegex("<(script|style)[^>]*>.*?</\\1>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ScriptOrStyleRegex();

    [GeneratedRegex("<[^>]+>", RegexOptions.IgnoreCase)]
    private static partial Regex HtmlTagRegex();
}

public sealed class UnsupportedDocumentTextExtractor(FileType fileType) : IDocumentTextExtractor
{
    public Task<string> ExtractAsync(string filePath, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException($"Document text extraction is not supported for {fileType} files yet.");
}

public sealed class DocumentTextExtractorFactory(RawTextExtractor rawTextExtractor, HtmlTextExtractor htmlTextExtractor) : IDocumentTextExtractorFactory
{
    public IDocumentTextExtractor GetExtractor(FileType fileType, string extension, string? mimeType)
    {
        var normalizedExtension = extension.ToLowerInvariant();
        return fileType switch
        {
            FileType.PlainText => rawTextExtractor,
            FileType.Markdown => rawTextExtractor,
            FileType.Html => htmlTextExtractor,
            FileType.Unknown when normalizedExtension is ".txt" => rawTextExtractor,
            FileType.Unknown when normalizedExtension is ".md" or ".markdown" => rawTextExtractor,
            FileType.Unknown when normalizedExtension is ".html" or ".htm" => htmlTextExtractor,
            _ => new UnsupportedDocumentTextExtractor(fileType),
        };
    }
}

public sealed class StubEmbeddingGenerator : IEmbeddingGenerator
{
    public Task<float[]> GenerateAsync(string text, CancellationToken cancellationToken = default)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Task.FromResult(bytes.Select(x => (float)x / byte.MaxValue).ToArray());
    }
}

public sealed class StubChatModelClient : IChatModelClient
{
    public Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken = default) => Task.FromResult("Local AI runtime is not configured yet.");
}

public sealed class ExactVectorSearchService(AppDbContext dbContext) : IVectorSearchService, IVectorIndex
{
    public Task UpsertAsync(Guid chunkId, float[] vector, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public async Task<IReadOnlyList<RagSourceDto>> SearchAsync(float[] queryVector, int limit, CancellationToken cancellationToken = default)
    {
        return await dbContext.DocumentChunks
            .OrderBy(x => x.Index)
            .Take(limit)
            .Select(x => new RagSourceDto(x.DocumentId, "Document", x.Id, x.PageNumber, 0, x.Text))
            .ToArrayAsync(cancellationToken);
    }
}

public sealed class RagContextBuilder(IVectorSearchService search, IEmbeddingGenerator embeddings) : IRagContextBuilder
{
    public async Task<IReadOnlyList<RagSourceDto>> BuildAsync(string question, CancellationToken cancellationToken = default)
    {
        var vector = await embeddings.GenerateAsync(question, cancellationToken);
        return await search.SearchAsync(vector, 8, cancellationToken);
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
