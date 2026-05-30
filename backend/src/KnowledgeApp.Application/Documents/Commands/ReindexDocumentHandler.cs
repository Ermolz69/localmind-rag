using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Documents;
using KnowledgeApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Documents;

public sealed class ReindexDocumentHandler(
    IDocumentRepository documentRepository,
    IDomainEventPublisher eventPublisher,
    IDateTimeProvider dateTimeProvider)
{
    public async Task<Result<ReindexDocumentResponse>> HandleAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        Document? document = await documentRepository.GetByIdAsync(documentId, cancellationToken);
        if (document is null)
        {
            return Result<ReindexDocumentResponse>.Failure(
                ApplicationErrors.NotFound(ErrorCodes.Documents.NotFound, "Document was not found."));
        }

        await eventPublisher.PublishAsync(new DocumentReindexRequestedEvent(documentId, dateTimeProvider.UtcNow), cancellationToken);
        return Result<ReindexDocumentResponse>.Success(new ReindexDocumentResponse(documentId, null, "Pending"));
    }
}
