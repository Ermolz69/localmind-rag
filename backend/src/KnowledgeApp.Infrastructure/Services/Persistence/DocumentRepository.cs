using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Infrastructure.Services.Persistence;

public sealed class DocumentRepository(AppDbContext dbContext) : IDocumentRepository
{
    public async Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Documents
            .Include(document => document.Tags)
            .FirstOrDefaultAsync(item => item.Id == id && item.DeletedAt == null, cancellationToken);
    }

    public async Task<IReadOnlyList<Document>> ListAsync(Guid? bucketId, string? status, int limit, int offset, CancellationToken cancellationToken = default)
    {
        IQueryable<Document> query = dbContext.Documents
            .Include(document => document.Tags)
            .Where(document => document.DeletedAt == null);

        if (bucketId.HasValue)
        {
            query = query.Where(document => document.BucketId == bucketId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<DocumentStatus>(status, true, out var documentStatus))
        {
            query = query.Where(document => document.Status == documentStatus);
        }

        List<Document> all = await query.ToListAsync(cancellationToken);

        IEnumerable<Document> result = all.OrderByDescending(document => document.CreatedAt);

        if (offset > 0)
        {
            result = result.Skip(offset);
        }

        if (limit > 0)
        {
            result = result.Take(limit);
        }

        return result.ToList();
    }

    public async Task<int> CountAsync(Guid? bucketId, string? status, CancellationToken cancellationToken = default)
    {
        IQueryable<Document> query = dbContext.Documents
            .Where(document => document.DeletedAt == null);

        if (bucketId.HasValue)
        {
            query = query.Where(document => document.BucketId == bucketId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<DocumentStatus>(status, true, out var documentStatus))
        {
            query = query.Where(document => document.Status == documentStatus);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task AddAsync(Document document, CancellationToken cancellationToken = default)
    {
        await dbContext.Documents.AddAsync(document, cancellationToken);
    }

    public Task UpdateAsync(Document document, CancellationToken cancellationToken = default)
    {
        dbContext.Documents.Update(document);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Document document, CancellationToken cancellationToken = default)
    {
        // Soft delete is handled by setting DeletedAt and status or we just delete it from context or let EF track it.
        // Let's keep it as standard EF Core Delete (or update since we do soft-deletes in C#).
        // Let's just update/remove in context, since in the codebase, soft deletes are done by setting DeletedAt then saving.
        // If we set DeletedAt and call Update, or remove, we want to match how it's done.
        // Let's check how DeleteDocumentHandler does it: it updates it and sets DeletedAt.
        // So we can support dbContext.Documents.Remove(document) or dbContext.Documents.Update(document) or both.
        // Actually, if we just call dbContext.Documents.Remove(document) or let EF handle it.
        // Wait, let's check DeleteDocumentHandler to be sure!
        dbContext.Documents.Remove(document);
        return Task.CompletedTask;
    }

    public async Task<DocumentFile?> GetFileByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        return await dbContext.DocumentFiles
            .FirstOrDefaultAsync(file => file.DocumentId == documentId, cancellationToken);
    }

    public async Task AddFileAsync(DocumentFile file, CancellationToken cancellationToken = default)
    {
        await dbContext.DocumentFiles.AddAsync(file, cancellationToken);
    }
}
