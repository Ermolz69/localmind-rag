using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Documents;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Documents;

public sealed class GetDocumentByIdHandler(
    IDocumentRepository documentRepository,
    IIngestionJobRepository ingestionJobs)
{
    public async Task<Result<DocumentDto>> HandleAsync(GetDocumentByIdQuery query, CancellationToken cancellationToken = default)
    {
        Domain.Entities.Document? document = await documentRepository.GetByIdAsync(query.DocumentId, cancellationToken);

        if (document is null)
        {
            return Result<DocumentDto>.Failure(ApplicationErrors.NotFound(ErrorCodes.Documents.NotFound, "Document was not found."));
        }

        IReadOnlyList<Domain.Entities.IngestionJob> failedJobs = await ingestionJobs.GetFailedJobsForDocumentsAsync([document.Id], cancellationToken);

        var lastError = failedJobs
            .OrderByDescending(job => job.ProcessedAt ?? job.CreatedAt)
            .Select(job => job.ErrorMessage)
            .FirstOrDefault();

        var tags = document.Tags?.Count > 0 
            ? (IReadOnlyDictionary<string, string>)document.Tags.ToDictionary(t => t.Key, t => t.Value) 
            : null;

        return Result<DocumentDto>.Success(new DocumentDto(
            document.Id,
            document.Name,
            document.Status.ToString(),
            document.CreatedAt,
            lastError,
            tags));
    }
}
