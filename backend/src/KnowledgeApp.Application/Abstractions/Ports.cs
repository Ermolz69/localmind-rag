using KnowledgeApp.Contracts.Documents;
using KnowledgeApp.Contracts.Rag;
using KnowledgeApp.Contracts.Runtime;
using KnowledgeApp.Domain.Enums;

namespace KnowledgeApp.Application.Abstractions;

public interface IAiModelRegistry { Task<IReadOnlyCollection<string>> ListModelsAsync(CancellationToken cancellationToken = default); }
public interface IAiRuntimeManager { Task<RuntimeStatusDto> GetStatusAsync(CancellationToken cancellationToken = default); Task StartAsync(CancellationToken cancellationToken = default); }
public interface IAppLockService { Task<bool> TryAcquireAsync(string key, CancellationToken cancellationToken = default); }
public interface IAppPathProvider { string DataDirectory { get; } string DatabasePath { get; } string FilesDirectory { get; } string IndexDirectory { get; } string LogsDirectory { get; } }
public interface IChatModelClient { Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken = default); }
public interface ICurrentUserService { Guid? UserId { get; } }
public interface IDateTimeProvider { DateTimeOffset UtcNow { get; } }
public interface IDocumentChunker { IReadOnlyList<string> Split(string text); }
public interface IDocumentTextExtractor { Task<string> ExtractAsync(string filePath, CancellationToken cancellationToken = default); }
public interface IDocumentTextExtractorFactory { IDocumentTextExtractor GetExtractor(FileType fileType, string extension, string? mimeType); }
public interface IEmbeddingGenerator { Task<float[]> GenerateAsync(string text, CancellationToken cancellationToken = default); }
public interface IFileStorageService { Task<StoredFileDto> SaveAsync(Stream content, Guid documentId, string fileName, CancellationToken cancellationToken = default); }
public interface IIngestionJobProcessor { Task ProcessAsync(Guid jobId, CancellationToken cancellationToken = default); }
public interface IIngestionQueue { Task EnqueueAsync(Guid documentId, CancellationToken cancellationToken = default); }
public interface INetworkStatusService { Task<bool> IsOnlineAsync(CancellationToken cancellationToken = default); }
public interface IRagAnswerGenerator { Task<RagAnswerDto> AnswerAsync(Guid conversationId, string question, CancellationToken cancellationToken = default); }
public interface IRagContextBuilder { Task<IReadOnlyList<RagSourceDto>> BuildAsync(string question, CancellationToken cancellationToken = default); }
public interface ISyncClient { Task PushAsync(CancellationToken cancellationToken = default); Task PullAsync(CancellationToken cancellationToken = default); }
public interface ISyncQueue { Task EnqueueAsync(Guid entityId, SyncOperation operation, CancellationToken cancellationToken = default); }
public interface ISyncService { Task<SyncStatusDto> GetStatusAsync(CancellationToken cancellationToken = default); Task RunAsync(CancellationToken cancellationToken = default); }
public interface IUnitOfWork { Task<int> SaveChangesAsync(CancellationToken cancellationToken = default); }
public interface IVectorIndex { Task UpsertAsync(Guid chunkId, float[] vector, CancellationToken cancellationToken = default); }
public interface IVectorSearchService { Task<IReadOnlyList<RagSourceDto>> SearchAsync(float[] queryVector, int limit, CancellationToken cancellationToken = default); }
