namespace KnowledgeApp.Contracts.Companion;

/// <summary>
/// What a trusted companion device is allowed to do. Only safe capabilities are
/// grantable; dangerous actions (deleting documents, changing system paths or AI
/// runtime, managing the whole configuration) are never available to a phone.
/// </summary>
/// <param name="Chat">Use the RAG chat.</param>
/// <param name="Search">Search the knowledge base.</param>
/// <param name="ViewDocuments">View documents and their statuses.</param>
/// <param name="ViewStatus">View indexing and watched-folder status.</param>
/// <param name="Rescan">Trigger watched-folder rescans.</param>
/// <param name="AddFiles">Add files from allowed folders.</param>
public sealed record CompanionDevicePermissionsDto(
    bool Chat,
    bool Search,
    bool ViewDocuments,
    bool ViewStatus,
    bool Rescan,
    bool AddFiles);
