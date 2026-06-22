namespace KnowledgeApp.Contracts.Companion;

/// <summary>A folder the phone is allowed to browse (a configured allowed root).</summary>
/// <param name="Name">Display name (the folder's leaf name).</param>
/// <param name="Path">Absolute path of the allowed root.</param>
public sealed record CompanionFileRootDto(string Name, string Path);

/// <summary>The set of allowed roots the phone may browse.</summary>
/// <param name="Roots">Configured allowed roots.</param>
public sealed record CompanionRootsResponse(IReadOnlyList<CompanionFileRootDto> Roots);

/// <summary>An entry inside an allowed folder: a subfolder or an addable file.</summary>
/// <param name="Name">File or folder name.</param>
/// <param name="Path">Absolute path of the entry.</param>
/// <param name="IsDirectory">True when the entry is a folder.</param>
public sealed record CompanionFileEntryDto(string Name, string Path, bool IsDirectory);

/// <summary>The contents of a browsed allowed folder.</summary>
/// <param name="Path">The folder being browsed.</param>
/// <param name="ParentPath">Parent folder when still inside an allowed root, else null.</param>
/// <param name="Entries">Subfolders and supported files within the folder.</param>
public sealed record CompanionBrowseResponse(
    string Path,
    string? ParentPath,
    IReadOnlyList<CompanionFileEntryDto> Entries);

/// <summary>Request to add a file from an allowed folder into LocalMind.</summary>
/// <param name="Path">Absolute path of the file to add. Must be inside an allowed root.</param>
public sealed record AddCompanionFileRequest(string Path);
