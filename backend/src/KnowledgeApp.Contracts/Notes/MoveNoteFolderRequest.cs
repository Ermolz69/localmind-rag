namespace KnowledgeApp.Contracts.Notes;

/// <summary>Request used to move a note folder to a new bucket and/or parent folder.</summary>
/// <param name="BucketId">The destination bucket id.</param>
/// <param name="ParentFolderId">The optional destination parent folder id.</param>
public sealed record MoveNoteFolderRequest(Guid BucketId, Guid? ParentFolderId);
