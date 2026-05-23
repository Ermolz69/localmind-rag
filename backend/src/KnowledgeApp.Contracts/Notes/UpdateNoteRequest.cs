namespace KnowledgeApp.Contracts.Notes;

/// <summary>Request used to update a local note.</summary>
/// <param name="Title">Updated note title.</param>
/// <param name="Markdown">Updated markdown note body.</param>
public sealed record UpdateNoteRequest(string Title, string Markdown);
