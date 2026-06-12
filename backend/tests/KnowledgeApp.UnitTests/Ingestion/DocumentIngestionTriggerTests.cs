using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Documents;
using KnowledgeApp.Application.Ingestion.EventHandlers;

namespace KnowledgeApp.UnitTests.Ingestion;

public sealed class DocumentIngestionTriggerTests
{
    [Fact]
    public async Task Uploaded_Event_Should_Enqueue_Document()
    {
        FakeIngestionQueue queue = new();
        DocumentIngestionTrigger trigger = new(queue);
        Guid documentId = Guid.NewGuid();

        await trigger.Handle(
            new DocumentUploadedEvent(documentId, DateTimeOffset.UtcNow),
            CancellationToken.None);

        Assert.Equal([documentId], queue.DocumentIds);
    }

    [Fact]
    public async Task Reindex_Event_Should_Enqueue_Document()
    {
        FakeIngestionQueue queue = new();
        DocumentIngestionTrigger trigger = new(queue);
        Guid documentId = Guid.NewGuid();

        await trigger.Handle(
            new DocumentReindexRequestedEvent(documentId, DateTimeOffset.UtcNow),
            CancellationToken.None);

        Assert.Equal([documentId], queue.DocumentIds);
    }

    private sealed class FakeIngestionQueue : IIngestionQueue
    {
        public List<Guid> DocumentIds { get; } = [];

        public Task<Guid> EnqueueAsync(
            Guid documentId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            DocumentIds.Add(documentId);
            return Task.FromResult(Guid.NewGuid());
        }
    }
}
