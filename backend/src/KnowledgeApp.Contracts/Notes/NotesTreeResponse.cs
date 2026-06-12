using KnowledgeApp.Contracts.Buckets;

namespace KnowledgeApp.Contracts.Notes;

/// <summary>A complete tree of folders and notes for a specific bucket.</summary>
/// <param name="Bucket">The current bucket.</param>
/// <param name="Folders">All folders inside the bucket.</param>
/// <param name="Notes">All notes inside the bucket.</param>
public sealed record NotesTreeResponse(
    BucketDto Bucket,
    IReadOnlyCollection<NoteFolderDto> Folders,
    IReadOnlyCollection<NoteDto> Notes);
