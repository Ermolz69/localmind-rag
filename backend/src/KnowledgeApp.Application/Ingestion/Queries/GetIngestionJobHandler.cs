using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Ingestion;

namespace KnowledgeApp.Application.Ingestion;

public sealed class GetIngestionJobHandler(IIngestionJobRepository ingestionJobs)
{
    public async Task<Result<IngestionJobDto>> HandleAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        Domain.Entities.IngestionJob? job = await ingestionJobs.GetAsync(jobId, cancellationToken);

        return job is null
            ? Result<IngestionJobDto>.Failure(ApplicationErrors.NotFound(ErrorCodes.Ingestion.JobNotFound, ErrorMessages.Ingestion.JobNotFound))
            : Result<IngestionJobDto>.Success(IngestionJobMapper.ToDto(job));
    }
}
