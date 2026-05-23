namespace KnowledgeApp.Contracts.Documents;

/// <summary>Stored local file metadata.</summary>
/// <param name="FileName">Stored file name.</param>
/// <param name="LocalPath">Local file path managed by the backend.</param>
/// <param name="SizeBytes">File size in bytes.</param>
/// <param name="ContentHash">Content hash used for deduplication and integrity checks.</param>
public sealed record StoredFileDto(string FileName, string LocalPath, long SizeBytes, string ContentHash);

