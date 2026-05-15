using KnowledgeApp.Application.Documents;
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

        ReindexDocumentResult? reindexResult = await reindex.HandleAsync(document.Id);
        ReindexDocumentResult? missingReindexResult = await reindex.HandleAsync(Guid.NewGuid());
        DeleteDocumentResult? deleteResult = await delete.HandleAsync(document.Id);
        DeleteDocumentResult? missingDeleteResult = await delete.HandleAsync(Guid.NewGuid());

        Document? storedDocument = await database.Context.Documents.SingleAsync(item => item.Id == document.Id);
        IngestionJob? job = await database.Context.IngestionJobs.SingleAsync(item => item.DocumentId == document.Id);

        Assert.True(reindexResult.Found);
        Assert.NotNull(reindexResult.JobId);
        Assert.False(missingReindexResult.Found);
        Assert.Null(missingReindexResult.JobId);
        Assert.True(deleteResult.Found);
        Assert.False(missingDeleteResult.Found);
        Assert.NotNull(storedDocument.DeletedAt);
        Assert.Equal(DocumentStatus.Deleted, storedDocument.Status);
        Assert.Equal(SyncStatus.DeletedLocal, storedDocument.SyncStatus);
        Assert.Equal(IngestionJobStatus.Queued, job.Status);
    }
}
