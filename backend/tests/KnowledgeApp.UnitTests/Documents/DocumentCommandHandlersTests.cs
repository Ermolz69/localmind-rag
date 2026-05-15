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
        await using var database = await ApplicationTestDatabase.CreateAsync();
        var document = new Document { Name = "notes.txt", Status = DocumentStatus.Indexed };
        database.Context.Documents.Add(document);
        await database.Context.SaveChangesAsync();

        var reindex = new ReindexDocumentHandler(database.Context);
        var delete = new DeleteDocumentHandler(database.Context);

        var reindexResult = await reindex.HandleAsync(document.Id);
        var missingReindexResult = await reindex.HandleAsync(Guid.NewGuid());
        var deleteResult = await delete.HandleAsync(document.Id);
        var missingDeleteResult = await delete.HandleAsync(Guid.NewGuid());

        var storedDocument = await database.Context.Documents.SingleAsync(item => item.Id == document.Id);
        var job = await database.Context.IngestionJobs.SingleAsync(item => item.DocumentId == document.Id);

        Assert.True(reindexResult.Found);
        Assert.NotNull(reindexResult.JobId);
        Assert.False(missingReindexResult.Found);
        Assert.Null(missingReindexResult.JobId);
        Assert.True(deleteResult.Found);
        Assert.False(missingDeleteResult.Found);
        Assert.Equal(DocumentStatus.Deleted, storedDocument.Status);
        Assert.Equal(SyncStatus.DeletedLocal, storedDocument.SyncStatus);
        Assert.Equal(IngestionJobStatus.Queued, job.Status);
    }
}
