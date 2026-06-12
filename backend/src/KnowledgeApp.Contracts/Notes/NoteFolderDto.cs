namespace KnowledgeApp.Contracts.Notes;

/// <summary>Folder returned by note folder endpoints.</summary>
/// <param name="Id">Local folder identifier.</param>
/// <param name="BucketId">The bucket containing the folder.</param>
/// <param name="ParentFolderId">Optional parent folder.</param>
/// <param name="Name">Folder name.</param>
/// <param name="SyncStatus">Current synchronization status code.</param>
/// <param name="CreatedAt">UTC creation timestamp.</param>
/// <param name="UpdatedAt">UTC timestamp of the latest update, when available.</param>
public sealed record NoteFolderDto(
    Guid Id,
    Guid BucketId,
    Guid? ParentFolderId,
    string Name,
    int SyncStatus,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
