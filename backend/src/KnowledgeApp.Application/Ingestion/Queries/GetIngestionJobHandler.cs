using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Ingestion;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Ingestion;

public sealed class GetIngestionJobHandler(IAppDbContext dbContext)
{
    public async Task<Result<IngestionJobDto>> HandleAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        Domain.Entities.IngestionJob? job = await dbContext.IngestionJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == jobId, cancellationToken);

        return job is null
            ? Result<IngestionJobDto>.Failure(ApplicationErrors.NotFound(ErrorCodes.Ingestion.JobNotFound, ErrorMessages.Ingestion.JobNotFound))
            : Result<IngestionJobDto>.Success(IngestionJobMapper.ToDto(job));
    }
}
