using KnowledgeApp.Application.Notes;
using KnowledgeApp.Contracts.Notes;
using KnowledgeApp.UnitTests;

namespace KnowledgeApp.UnitTests.Notes;

public sealed class NoteHandlersTests
{
    [Fact]
    public async Task NoteHandlers_Should_List_Create_Update_And_Delete()
    {
        await using ApplicationTestDatabase? database = await ApplicationTestDatabase.CreateAsync();
        NoteRequestValidator validator = new();
        CreateNoteHandler create = new(database.Context, validator);
        GetNotesHandler list = new(database.Context);
        UpdateNoteHandler update = new(database.Context, validator);
        DeleteNoteHandler delete = new(database.Context);

        NoteDto? note = await create.HandleAsync(new CreateNoteRequest(BucketId: null, "Draft", "Body"));
        Contracts.Common.CursorPage<NoteDto> notes = await list.HandleAsync(new GetNotesQuery());
        UpdateNoteResult? updateResult = await update.HandleAsync(note.Id, new UpdateNoteRequest("Done", "Updated"));
        UpdateNoteResult? missingUpdateResult = await update.HandleAsync(Guid.NewGuid(), new UpdateNoteRequest("Missing", "Body"));
        DeleteNoteResult? deleteResult = await delete.HandleAsync(note.Id);
        DeleteNoteResult? missingDeleteResult = await delete.HandleAsync(note.Id);

        Assert.Contains(notes.Items, item => item.Id == note.Id);
        Assert.True(updateResult.Found);
        Assert.False(missingUpdateResult.Found);
        Assert.True(deleteResult.Found);
        Assert.False(missingDeleteResult.Found);
    }
}
