using KnowledgeApp.Application.Notes;
using KnowledgeApp.Contracts.Notes;
using KnowledgeApp.UnitTests;

namespace KnowledgeApp.UnitTests.Notes;

public sealed class NoteHandlersTests
{
    [Fact]
    public async Task NoteHandlers_Should_List_Create_Update_And_Delete()
    {
        await using var database = await ApplicationTestDatabase.CreateAsync();
        var create = new CreateNoteHandler(database.Context);
        var list = new GetNotesHandler(database.Context);
        var update = new UpdateNoteHandler(database.Context);
        var delete = new DeleteNoteHandler(database.Context);

        var note = await create.HandleAsync(new CreateNoteRequest(BucketId: null, "Draft", "Body"));
        var notes = await list.HandleAsync();
        var updateResult = await update.HandleAsync(note.Id, new UpdateNoteRequest("Done", "Updated"));
        var missingUpdateResult = await update.HandleAsync(Guid.NewGuid(), new UpdateNoteRequest("Missing", "Body"));
        var deleteResult = await delete.HandleAsync(note.Id);
        var missingDeleteResult = await delete.HandleAsync(note.Id);

        Assert.Contains(notes, item => item.Id == note.Id);
        Assert.True(updateResult.Found);
        Assert.False(missingUpdateResult.Found);
        Assert.True(deleteResult.Found);
        Assert.False(missingDeleteResult.Found);
    }
}
