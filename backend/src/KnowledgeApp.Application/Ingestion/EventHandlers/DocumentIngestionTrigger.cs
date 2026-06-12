using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Documents;
using MediatR;

namespace KnowledgeApp.Application.Ingestion.EventHandlers;

public sealed class DocumentIngestionTrigger(IIngestionQueue ingestionQueue) :
    INotificationHandler<DocumentUploadedEvent>,
    INotificationHandler<DocumentReindexRequestedEvent>
{
    public async Task Handle(DocumentUploadedEvent notification, CancellationToken cancellationToken)
    {
        await ingestionQueue.EnqueueAsync(notification.DocumentId, cancellationToken);
    }

    public async Task Handle(DocumentReindexRequestedEvent notification, CancellationToken cancellationToken)
    {
        await ingestionQueue.EnqueueAsync(notification.DocumentId, cancellationToken);
    }
}
