using KnowledgeApp.Application.Common.Results;

namespace KnowledgeApp.Infrastructure.Services.DocumentPreview;

public sealed record DocumentPreviewProcessResult(string PdfPath);

public interface IDocumentPreviewConverterProcess
{
    Task<Result<DocumentPreviewProcessResult>> ConvertToPdfAsync(
        string sourcePath,
        string outputDirectory,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}
