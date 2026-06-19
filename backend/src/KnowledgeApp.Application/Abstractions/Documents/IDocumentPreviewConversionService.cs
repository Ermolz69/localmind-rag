using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Domain.Enums;

namespace KnowledgeApp.Application.Abstractions;

public sealed record DocumentPreviewConversionRequest(
    Guid DocumentId,
    string SourcePath,
    string SourceFileName,
    string ContentHash,
    FileType FileType);

public sealed record DocumentPreviewConversionResult(
    string PreviewFilePath,
    string PreviewFileName,
    string ContentType);

public interface IDocumentPreviewConversionService
{
    Task<Result<DocumentPreviewConversionResult>> GetOrCreatePdfPreviewAsync(
        DocumentPreviewConversionRequest request,
        CancellationToken cancellationToken = default);
}
