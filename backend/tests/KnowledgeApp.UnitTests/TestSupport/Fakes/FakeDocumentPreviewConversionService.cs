using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Results;

namespace KnowledgeApp.UnitTests.TestSupport.Fakes;

internal sealed class FakeDocumentPreviewConversionService : IDocumentPreviewConversionService
{
    public Task<Result<DocumentPreviewConversionResult>> GetOrCreatePdfPreviewAsync(
        DocumentPreviewConversionRequest request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Result<DocumentPreviewConversionResult>.Failure(
            ApplicationErrors.UnsupportedMedia(
                "DOCUMENT_PREVIEW_UNSUPPORTED",
                "Conversion is not available in unit tests.")));
}
