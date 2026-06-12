namespace KnowledgeApp.Contracts.Notes;

/// <summary>Request used to create a note folder.</summary>
/// <param name="BucketId">The bucket that should contain the folder.</param>
/// <param name="ParentFolderId">Optional parent folder.</param>
/// <param name="Name">Folder name.</param>
public sealed record CreateNoteFolderRequest(Guid BucketId, Guid? ParentFolderId, string Name);
