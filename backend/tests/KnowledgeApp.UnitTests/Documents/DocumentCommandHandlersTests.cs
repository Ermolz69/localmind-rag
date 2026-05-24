using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Application.Documents;
using KnowledgeApp.Contracts.Documents;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
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

        ReindexDocumentHandler? reindex = new ReindexDocumentHandler(database.Context);
        DeleteDocumentHandler? delete = new DeleteDocumentHandler(database.Context, new FixedDateTimeProvider());

        ReindexDocumentResponse? reindexResult = (await reindex.HandleAsync(document.Id)).AssertSuccess();
        Result<ReindexDocumentResponse> missingReindexResult = await reindex.HandleAsync(Guid.NewGuid());
        Result deleteResult = await delete.HandleAsync(document.Id);
        Result missingDeleteResult = await delete.HandleAsync(Guid.NewGuid());

        Document? storedDocument = await database.Context.Documents.SingleAsync(item => item.Id == document.Id);
        IngestionJob? job = await database.Context.IngestionJobs.SingleAsync(item => item.DocumentId == document.Id);

        Assert.NotEqual(Guid.Empty, reindexResult.IngestionJobId);
        Assert.Equal("DOCUMENT_NOT_FOUND", missingReindexResult.AssertFailure(ErrorType.NotFound).Code);
        deleteResult.AssertSuccess();
        Assert.Equal("DOCUMENT_NOT_FOUND", missingDeleteResult.AssertFailure(ErrorType.NotFound).Code);
        Assert.NotNull(storedDocument.DeletedAt);
        Assert.Equal(DocumentStatus.Deleted, storedDocument.Status);
        Assert.Equal(SyncStatus.DeletedLocal, storedDocument.SyncStatus);
        Assert.Equal(IngestionJobStatus.Queued, job.Status);
    }
}
