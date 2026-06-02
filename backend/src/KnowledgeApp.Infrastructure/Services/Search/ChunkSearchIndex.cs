using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Infrastructure.Services.Search;

public sealed class ChunkSearchIndex(AppDbContext dbContext) : IChunkSearchIndex
{
    public async Task<IReadOnlyList<DocumentChunkCandidate>> SearchDocumentCandidatesAsync(
        string[] terms,
        Guid? bucketId,
        Guid? documentId,
        IReadOnlyDictionary<string, string>? tags,
        int maxCount,
        CancellationToken cancellationToken = default)
    {
        var query =
            from document in dbContext.Documents.AsNoTracking()
            join chunk in dbContext.DocumentChunks.AsNoTracking()
                on document.Id equals chunk.DocumentId
            where document.DeletedAt == null &&
                  document.Status == DocumentStatus.Indexed
            select new
            {
                Document = document,
                Chunk = chunk
            };

        if (bucketId.HasValue)
        {
            query = query.Where(candidate => candidate.Document.BucketId == bucketId.Value);
        }

        if (documentId.HasValue)
        {
            query = query.Where(candidate => candidate.Document.Id == documentId.Value);
        }

        if (tags is { Count: > 0 })
        {
            foreach (var tag in tags)
            {
                query = query.Where(candidate =>
                    dbContext.DocumentTags.Any(dt => dt.DocumentId == candidate.Document.Id && dt.Key == tag.Key && dt.Value == tag.Value) ||
                    dbContext.DocumentChunkTags.Any(ct => ct.DocumentChunkId == candidate.Chunk.Id && ct.Key == tag.Key && ct.Value == tag.Value));
            }
        }

        foreach (string term in terms)
        {
            string searchTerm = term;
            query = query.Where(candidate =>
                candidate.Document.Name.Contains(searchTerm) ||
                candidate.Chunk.Text.Contains(searchTerm));
        }

        return await query
            .OrderBy(candidate => candidate.Document.Name)
            .ThenBy(candidate => candidate.Document.Id)
            .ThenBy(candidate => candidate.Chunk.Index)
            .Select(candidate => new DocumentChunkCandidate(
                candidate.Document.Id,
                candidate.Document.BucketId,
                candidate.Chunk.Id,
                candidate.Document.Name,
                candidate.Chunk.PageNumber,
                candidate.Chunk.Text))
            .Take(maxCount)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NoteCandidate>> SearchNoteCandidatesAsync(
        string[] terms,
        Guid? bucketId,
        Guid? noteId,
        IReadOnlyDictionary<string, string>? tags,
        int maxCount,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Notes
            .AsNoTracking()
            .Where(note => note.DeletedAt == null);

        if (bucketId.HasValue)
        {
            query = query.Where(note => note.BucketId == bucketId.Value);
        }

        if (noteId.HasValue)
        {
            query = query.Where(note => note.Id == noteId.Value);
        }

        if (tags is { Count: > 0 })
        {
            foreach (var tag in tags)
            {
                query = query.Where(note =>
                    dbContext.NoteTags.Any(nt => nt.NoteId == note.Id && nt.Key == tag.Key && nt.Value == tag.Value));
            }
        }

        foreach (string term in terms)
        {
            string searchTerm = term;
            query = query.Where(note =>
                note.Title.Contains(searchTerm) ||
                note.Markdown.Contains(searchTerm));
        }

        return await query
            .OrderBy(note => note.Title)
            .ThenBy(note => note.Id)
            .Select(note => new NoteCandidate(
                note.Id,
                note.BucketId,
                note.Title,
                note.Markdown))
            .Take(maxCount)
            .ToListAsync(cancellationToken);
    }
}
