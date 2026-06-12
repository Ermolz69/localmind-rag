namespace KnowledgeApp.Contracts.Notes;

/// <summary>Request used to move a note to a new bucket and/or folder.</summary>
/// <param name="BucketId">The destination bucket id.</param>
/// <param name="FolderId">The optional destination folder id.</param>
public sealed record MoveNoteRequest(Guid BucketId, Guid? FolderId);
