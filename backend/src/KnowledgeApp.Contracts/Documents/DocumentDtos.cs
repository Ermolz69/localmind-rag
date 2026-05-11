namespace KnowledgeApp.Contracts.Documents;

public sealed record DocumentDto(Guid Id, string Name, string Status, DateTimeOffset CreatedAt);
public sealed record StoredFileDto(string FileName, string LocalPath, long SizeBytes, string ContentHash);
public sealed record UploadDocumentResponse(Guid DocumentId, Guid IngestionJobId, string Status);
