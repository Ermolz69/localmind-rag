namespace KnowledgeApp.Contracts.Documents;

/// <summary>Read-only preview metadata and inline content for a local document.</summary>
/// <param name="DocumentId">Local document identifier.</param>
/// <param name="FileName">Stored document file name.</param>
/// <param name="ContentType">Preview content type.</param>
/// <param name="PreviewKind">Preview category that tells the frontend how to render the response.</param>
/// <param name="PreviewUrl">LocalApi URL for read-only file preview streams, when applicable.</param>
/// <param name="TextContent">Inline text content for safe text-based previews.</param>
/// <param name="ErrorCode">Stable error or unsupported-state code, when preview is unavailable.</param>
/// <param name="Message">Frontend-safe preview state message.</param>
public sealed record DocumentPreviewResponse(
    Guid DocumentId,
    string FileName,
    string ContentType,
    DocumentPreviewKind PreviewKind,
    string? PreviewUrl = null,
    string? TextContent = null,
    string? ErrorCode = null,
    string? Message = null);
