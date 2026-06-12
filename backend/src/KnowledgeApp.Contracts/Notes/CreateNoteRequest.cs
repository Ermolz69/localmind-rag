namespace KnowledgeApp.Contracts.Notes;

/// <summary>Request used to create a local note.</summary>
/// <param name="BucketId">The bucket that should contain the note.</param>
/// <param name="FolderId">Optional folder that should contain the note.</param>
/// <param name="Title">Note title.</param>
/// <param name="Markdown">Markdown note body.</param>
/// <param name="Tags">Optional metadata tags.</param>
public sealed record CreateNoteRequest(Guid BucketId, Guid? FolderId, string Title, string Markdown, IReadOnlyDictionary<string, string>? Tags = null);
