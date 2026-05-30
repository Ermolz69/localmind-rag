using KnowledgeApp.Domain.Entities;

namespace KnowledgeApp.Application.Abstractions;

public interface IChunkSearchIndex
{
    Task<IReadOnlyList<DocumentChunkCandidate>> SearchDocumentCandidatesAsync(string[] terms, Guid? bucketId, Guid? documentId, int maxCount, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NoteCandidate>> SearchNoteCandidatesAsync(string[] terms, Guid? bucketId, Guid? noteId, int maxCount, CancellationToken cancellationToken = default);
}

public sealed record DocumentChunkCandidate(
    Guid DocumentId,
    Guid? BucketId,
    Guid ChunkId,
    string Title,
    int? PageNumber,
    string Text);

public sealed record NoteCandidate(
    Guid NoteId,
    Guid? BucketId,
    string Title,
    string Markdown);
