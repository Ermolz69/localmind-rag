namespace KnowledgeApp.Contracts.Notes;

/// <summary>Request used to update a local note.</summary>
/// <param name="Title">Updated note title.</param>
/// <param name="Markdown">Updated markdown note body.</param>
/// <param name="Tags">Optional updated metadata tags.</param>
public sealed record UpdateNoteRequest(string Title, string Markdown, IReadOnlyDictionary<string, string>? Tags = null)
{
    /// <summary>Optional updated folder id.</summary>
    public Guid? FolderId { get; init; }
}
