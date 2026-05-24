using KnowledgeApp.Application.Notes;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Notes;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.UnitTests;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.UnitTests.Notes;

public sealed class NoteHandlersTests
{
    [Fact]
    public async Task NoteHandlers_Should_List_Create_Update_And_Delete()
    {
        await using ApplicationTestDatabase? database = await ApplicationTestDatabase.CreateAsync();
        NoteRequestValidator validator = new();
        CreateNoteHandler create = new(database.Context, validator, new FakeLocalDeviceResolver());
        GetNotesHandler list = new(database.Context);
        UpdateNoteHandler update = new(database.Context, validator);
        DeleteNoteHandler delete = new(database.Context, new FixedDateTimeProvider());

        NoteDto? note = (await create.HandleAsync(new CreateNoteRequest(BucketId: null, "Draft", "Body"))).AssertSuccess();
        Contracts.Common.CursorPage<NoteDto> notes = (await list.HandleAsync(new GetNotesQuery())).AssertSuccess();
        Result updateResult = await update.HandleAsync(note.Id, new UpdateNoteRequest("Done", "Updated"));
        Result missingUpdateResult = await update.HandleAsync(Guid.NewGuid(), new UpdateNoteRequest("Missing", "Body"));
        Result deleteResult = await delete.HandleAsync(note.Id);
        Result missingDeleteResult = await delete.HandleAsync(note.Id);
        Domain.Entities.Note storedNote = await database.Context.Notes.SingleAsync(item => item.Id == note.Id);
        Contracts.Common.CursorPage<NoteDto> visibleNotes = (await list.HandleAsync(new GetNotesQuery())).AssertSuccess();

        Assert.Contains(notes.Items, item => item.Id == note.Id);
        updateResult.AssertSuccess();
        Assert.Equal("NOTE_NOT_FOUND", missingUpdateResult.AssertFailure().Code);
        deleteResult.AssertSuccess();
        Assert.Equal("NOTE_NOT_FOUND", missingDeleteResult.AssertFailure().Code);
        Assert.NotNull(storedNote.DeletedAt);
        Assert.Equal(SyncStatus.DeletedLocal, storedNote.SyncStatus);
        Assert.DoesNotContain(visibleNotes.Items, item => item.Id == note.Id);
    }
}
