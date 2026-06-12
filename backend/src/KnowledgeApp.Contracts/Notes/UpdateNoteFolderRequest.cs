namespace KnowledgeApp.Contracts.Notes;

/// <summary>Request used to update a note folder.</summary>
/// <param name="Name">Folder name.</param>
public sealed record UpdateNoteFolderRequest(string Name)
{
    /// <summary>Optional parent folder for moving.</summary>
    public Guid? ParentFolderId { get; init; }
}
