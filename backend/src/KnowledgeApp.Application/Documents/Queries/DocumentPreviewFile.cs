namespace KnowledgeApp.Application.Documents;

public sealed record DocumentPreviewFile(
    string FilePath,
    string FileName,
    string ContentType);
