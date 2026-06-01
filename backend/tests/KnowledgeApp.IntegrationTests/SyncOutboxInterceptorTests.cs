using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeApp.IntegrationTests;

public sealed class SyncOutboxInterceptorTests(LocalApiTestFactory factory) : IClassFixture<LocalApiTestFactory>
{
    [Fact]
    public async Task Creating_Syncable_Entity_Generates_Outbox_Item_And_Sets_LocalVersion()
    {
        // Arrange
        using IServiceScope scope = factory.Services.CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var note = new Note
        {
            Title = "Sync Test Note",
            Markdown = "Hello World"
        };

        // Act
        dbContext.Notes.Add(note);
        await dbContext.SaveChangesAsync();

        // Assert
        Assert.Equal(1, note.LocalVersion);

        SyncOutboxItem? outboxItem = await dbContext.SyncOutbox
            .FirstOrDefaultAsync(x => x.EntityId == note.Id);

        Assert.NotNull(outboxItem);
        Assert.Equal("Note", outboxItem.EntityType);
        Assert.Equal(SyncOperation.CreateNote, outboxItem.Operation);
        Assert.Equal(SyncStatus.PendingUpload, outboxItem.Status);
        Assert.Contains("Sync Test Note", outboxItem.PayloadJson);

        // Act 2 - Update
        note.Title = "Updated Title";
        await dbContext.SaveChangesAsync();

        // Assert 2
        Assert.Equal(2, note.LocalVersion);

        List<SyncOutboxItem> updates = await dbContext.SyncOutbox
            .Where(x => x.EntityId == note.Id && x.Operation == SyncOperation.UpdateNote)
            .ToListAsync();

        Assert.Single(updates);
        Assert.Contains("Updated Title", updates[0].PayloadJson);
    }
}
