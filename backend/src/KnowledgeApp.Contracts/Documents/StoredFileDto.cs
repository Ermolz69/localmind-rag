namespace KnowledgeApp.Contracts.Documents;

public sealed record StoredFileDto(string FileName, string LocalPath, long SizeBytes, string ContentHash);


