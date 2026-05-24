using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Documents;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Documents;

public sealed class GetDocumentByIdHandler(IAppDbContext dbContext)
{
    public async Task<Result<DocumentDto>> HandleAsync(GetDocumentByIdQuery query, CancellationToken cancellationToken = default)
    {
        Domain.Entities.Document? document = await dbContext.Documents
            .AsNoTracking()
            .Where(document => document.Id == query.DocumentId && document.DeletedAt == null)
            .SingleOrDefaultAsync(cancellationToken);

        if (document is null)
        {
            return Result<DocumentDto>.Failure(ApplicationErrors.NotFound(ErrorCodes.Documents.NotFound, "Document was not found."));
        }

        Domain.Entities.IngestionJob[]? failedJobs = await dbContext.IngestionJobs
            .AsNoTracking()
            .Where(job => job.DocumentId == document.Id && job.LastError != null)
            .ToArrayAsync(cancellationToken);

        return Result<DocumentDto>.Success(new DocumentDto(
            document.Id,
            document.Name,
            document.Status.ToString(),
            document.CreatedAt,
            failedJobs
                .OrderByDescending(job => job.ProcessedAt ?? job.CreatedAt)
                .Select(job => job.LastError)
                .FirstOrDefault()));
    }
}
