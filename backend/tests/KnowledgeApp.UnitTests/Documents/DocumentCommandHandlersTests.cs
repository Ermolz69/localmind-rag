using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Application.Documents;
using KnowledgeApp.Contracts.Documents;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Services;
using KnowledgeApp.UnitTests;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.UnitTests.Documents;

public sealed class DocumentCommandHandlersTests
{
    [Fact]
    public async Task DocumentHandlers_Should_Delete_And_Reindex_Documents()
    {
        await using ApplicationTestDatabase? database = await ApplicationTestDatabase.CreateAsync();
        Document? document = new Document { Name = "notes.txt", Status = DocumentStatus.Indexed };
        database.Context.Documents.Add(document);
        await database.Context.SaveChangesAsync();

        var documentRepository = new KnowledgeApp.Infrastructure.Services.Persistence.DocumentRepository(database.Context);
        var unitOfWork = new KnowledgeApp.Infrastructure.Services.UnitOfWork(database.Context);

        var publisher = new FakeDomainEventPublisher();

        ReindexDocumentHandler reindex = new ReindexDocumentHandler(
            documentRepository,
            publisher,
            new FixedDateTimeProvider());
        DeleteDocumentHandler delete = new DeleteDocumentHandler(
            documentRepository,
            unitOfWork,
            new FixedDateTimeProvider());

        ReindexDocumentResponse? reindexResult = (await reindex.HandleAsync(document.Id)).AssertSuccess();
        Result<ReindexDocumentResponse> missingReindexResult = await reindex.HandleAsync(Guid.NewGuid());
        Result deleteResult = await delete.HandleAsync(document.Id);
        Result missingDeleteResult = await delete.HandleAsync(Guid.NewGuid());

        Document? storedDocument = await database.Context.Documents.SingleAsync(item => item.Id == document.Id);

        Assert.Null(reindexResult.IngestionJobId);
        Assert.Equal("DOCUMENT_NOT_FOUND", missingReindexResult.AssertFailure(ErrorType.NotFound).Code);
        deleteResult.AssertSuccess();
        Assert.Equal("DOCUMENT_NOT_FOUND", missingDeleteResult.AssertFailure(ErrorType.NotFound).Code);
        Assert.NotNull(storedDocument.DeletedAt);
        Assert.Equal(DocumentStatus.Deleted, storedDocument.Status);
        Assert.Equal(SyncStatus.DeletedLocal, storedDocument.SyncStatus);
        Assert.Single(publisher.Events);
        Assert.IsType<KnowledgeApp.Application.Documents.DocumentReindexRequestedEvent>(publisher.Events.Single());
    }

    private sealed class FakeDomainEventPublisher : KnowledgeApp.Application.Abstractions.IDomainEventPublisher
    {
        public List<KnowledgeApp.Application.Abstractions.IDomainEvent> Events { get; } = new();

        public Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default) where TEvent : KnowledgeApp.Application.Abstractions.IDomainEvent
        {
            Events.Add(domainEvent);
            return Task.CompletedTask;
        }
    }
}
