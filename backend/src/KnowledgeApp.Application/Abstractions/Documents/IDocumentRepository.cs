using KnowledgeApp.Domain.Entities;

namespace KnowledgeApp.Application.Abstractions;

public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Document>> ListAsync(Guid? bucketId, string? status, DateTimeOffset? cursorCreatedAt, Guid? cursorId, int limit, CancellationToken cancellationToken = default);
    Task<int> CountAsync(Guid? bucketId, string? status, CancellationToken cancellationToken = default);
    Task AddAsync(Document document, CancellationToken cancellationToken = default);
    Task UpdateAsync(Document document, CancellationToken cancellationToken = default);
    Task DeleteAsync(Document document, CancellationToken cancellationToken = default);

    Task<DocumentFile?> GetFileByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task AddFileAsync(DocumentFile file, CancellationToken cancellationToken = default);
}
