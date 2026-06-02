namespace KnowledgeApp.Contracts.Notes;

/// <summary>Note returned by note endpoints.</summary>
/// <param name="Id">Local note identifier.</param>
/// <param name="BucketId">Optional bucket containing the note.</param>
/// <param name="Title">Note title.</param>
/// <param name="Markdown">Markdown note body.</param>
/// <param name="SyncStatus">Current synchronization status code.</param>
/// <param name="CreatedAt">UTC creation timestamp.</param>
/// <param name="UpdatedAt">UTC timestamp of the latest update, when available.</param>
/// <param name="Tags">Optional metadata tags.</param>
public sealed record NoteDto(
    Guid Id,
    Guid? BucketId,
    string Title,
    string Markdown,
    int SyncStatus,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    IReadOnlyDictionary<string, string>? Tags = null);
