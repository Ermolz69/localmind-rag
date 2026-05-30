using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Infrastructure.Services.Persistence;

public sealed class EmbeddingStore(AppDbContext dbContext) : IEmbeddingStore
{
    public async Task<IReadOnlyList<DocumentEmbedding>> GetEmbeddingsByChunkIdsAsync(IReadOnlyList<Guid> chunkIds, CancellationToken cancellationToken = default)
    {
        return await dbContext.DocumentEmbeddings
            .Where(embedding => chunkIds.Contains(embedding.DocumentChunkId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DocumentEmbedding>> GetEmbeddingsForExactSearchAsync(CancellationToken cancellationToken = default)
    {
        // Join with non-deleted documents to verify active document status
        return await dbContext.DocumentEmbeddings
            .AsNoTracking()
            .Join(
                dbContext.DocumentChunks.AsNoTracking(),
                emb => emb.DocumentChunkId,
                chk => chk.Id,
                (emb, chk) => new { emb, chk }
            )
            .Join(
                dbContext.Documents.AsNoTracking().Where(doc => doc.DeletedAt == null),
                combined => combined.chk.DocumentId,
                doc => doc.Id,
                (combined, doc) => combined.emb
            )
            .ToListAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IReadOnlyList<DocumentEmbedding> embeddings, CancellationToken cancellationToken = default)
    {
        await dbContext.DocumentEmbeddings.AddRangeAsync(embeddings, cancellationToken);
    }

    public Task RemoveRangeAsync(IReadOnlyList<DocumentEmbedding> embeddings, CancellationToken cancellationToken = default)
    {
        dbContext.DocumentEmbeddings.RemoveRange(embeddings);
        return Task.CompletedTask;
    }
}
