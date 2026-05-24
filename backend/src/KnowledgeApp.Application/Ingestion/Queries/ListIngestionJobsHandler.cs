using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Ingestion;
using KnowledgeApp.Domain.Enums;

namespace KnowledgeApp.Application.Ingestion;

public sealed record ListIngestionJobsQuery(string? Status = null, int Limit = 50, int Offset = 0);

public sealed class ListIngestionJobsHandler(IIngestionJobRepository ingestionJobs)
{
    public async Task<Result<IngestionJobListResponse>> HandleAsync(
        ListIngestionJobsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.Limit < 1 || query.Limit > 200)
        {
            return Result<IngestionJobListResponse>.Failure(ApplicationErrors.Validation(
                ErrorCodes.Pagination.InvalidLimit,
                ErrorMessages.Pagination.InvalidLimit,
                new Dictionary<string, string[]> { ["limit"] = [ErrorMessages.Pagination.LimitOutOfRange] }));
        }

        if (query.Offset < 0)
        {
            return Result<IngestionJobListResponse>.Failure(ApplicationErrors.Validation(
                ErrorCodes.Pagination.InvalidLimit,
                ErrorMessages.Pagination.InvalidLimit,
                new Dictionary<string, string[]> { ["offset"] = ["Offset must be greater than or equal to 0."] }));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!Enum.TryParse(query.Status, ignoreCase: true, out IngestionJobStatus _))
            {
                return Result<IngestionJobListResponse>.Failure(ApplicationErrors.Validation(
                    ErrorCodes.RequestInvalid,
                    "Ingestion status filter is invalid.",
                    new Dictionary<string, string[]> { ["status"] = ["Ingestion status filter is invalid."] }));
            }
        }

        int total = await ingestionJobs.CountAsync(query.Status, cancellationToken);
        IReadOnlyList<Domain.Entities.IngestionJob> jobRows = await ingestionJobs.ListAsync(
            query.Status,
            query.Limit,
            query.Offset,
            cancellationToken);
        IngestionJobDto[] items = jobRows.Select(IngestionJobMapper.ToDto).ToArray();

        return Result<IngestionJobListResponse>.Success(new IngestionJobListResponse(items, total, query.Limit, query.Offset));
    }
}
