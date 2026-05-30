using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Documents;
using KnowledgeApp.Domain.Entities;
using MediatR;

namespace KnowledgeApp.Application.Ingestion.EventHandlers;

public sealed class DocumentIngestionTrigger(
    IIngestionJobRepository ingestionJobs,
    IUnitOfWork unitOfWork) : 
    INotificationHandler<DocumentUploadedEvent>,
    INotificationHandler<DocumentReindexRequestedEvent>
{
    public async Task Handle(DocumentUploadedEvent notification, CancellationToken cancellationToken)
    {
        await ingestionJobs.CreatePendingAsync(notification.DocumentId, notification.Timestamp, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(DocumentReindexRequestedEvent notification, CancellationToken cancellationToken)
    {
        await ingestionJobs.CreatePendingAsync(notification.DocumentId, notification.Timestamp, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
