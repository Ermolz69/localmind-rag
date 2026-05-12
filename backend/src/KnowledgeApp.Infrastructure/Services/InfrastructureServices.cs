using System.Security.Cryptography;
using System.Text;
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

public sealed class NoopIngestionJobProcessor : IIngestionJobProcessor
{
    public Task ProcessAsync(Guid jobId, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

public sealed class SimpleDocumentChunker : IDocumentChunker
{
    public IReadOnlyList<string> Split(string text) => text.Chunk(1200).Select(chars => new string(chars)).ToArray();
}

public sealed class PlainTextExtractor : IDocumentTextExtractor
{
    public Task<string> ExtractAsync(string filePath, CancellationToken cancellationToken = default) => File.ReadAllTextAsync(filePath, cancellationToken);
}

public sealed class DocumentTextExtractorFactory(PlainTextExtractor plainTextExtractor) : IDocumentTextExtractorFactory
{
    public IDocumentTextExtractor GetExtractor(FileType fileType, string extension, string? mimeType) => plainTextExtractor;
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
