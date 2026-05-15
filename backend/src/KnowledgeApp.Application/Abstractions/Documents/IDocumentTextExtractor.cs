using KnowledgeApp.Domain.Enums;

namespace KnowledgeApp.Application.Abstractions;

public sealed record DocumentTextExtractionResult(IReadOnlyList<DocumentTextSegment> Segments);

public sealed record DocumentTextSegment(
    string Text,
    int? PageNumber = null,
    string? SectionTitle = null,
    string SourceKind = "Document");

public interface IDocumentTextExtractor
{
    Task<DocumentTextExtractionResult> ExtractAsync(string filePath, CancellationToken cancellationToken = default);
}

public interface IDocumentTextExtractorFactory
{
    IDocumentTextExtractor GetExtractor(FileType fileType, string extension, string? mimeType);
}
