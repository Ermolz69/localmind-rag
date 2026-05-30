using KnowledgeApp.Application.Common.Diagnostics;
using KnowledgeApp.Contracts.Common;
using KnowledgeApp.Contracts.Diagnostics;
using KnowledgeApp.Domain.Entities;
using MediatR;

namespace KnowledgeApp.Application.Diagnostics.Queries;

public record GetOperationLogsQuery(int Limit = 50, string? Cursor = null) : IRequest<CursorPage<OperationLogDto>>;

public class GetOperationLogsHandler : IRequestHandler<GetOperationLogsQuery, CursorPage<OperationLogDto>>
{
    private readonly IOperationLogRepository _repository;

    public GetOperationLogsHandler(IOperationLogRepository repository)
    {
        _repository = repository;
    }

    public async Task<CursorPage<OperationLogDto>> Handle(GetOperationLogsQuery request, CancellationToken cancellationToken)
    {
        // Fetch Limit + 1 to know if there is a next page
        int fetchCount = request.Limit + 1;
        IReadOnlyList<OperationLog> logs = await _repository.GetRecentLogsAsync(fetchCount, request.Cursor, cancellationToken);

        bool hasNextPage = logs.Count > request.Limit;
        IEnumerable<OperationLog> items = logs.Take(request.Limit);

        List<OperationLogDto> dtos = items.Select(x => new OperationLogDto(
            x.Id,
            x.OperationType,
            x.EntityType,
            x.EntityId,
            x.Message,
            x.MetadataJson,
            x.TraceId,
            x.CreatedAt)).ToList();

        string? nextCursor = hasNextPage ? dtos.Last().Id : null;

        return new CursorPage<OperationLogDto>(dtos, nextCursor, request.Limit, hasNextPage);
    }
}
