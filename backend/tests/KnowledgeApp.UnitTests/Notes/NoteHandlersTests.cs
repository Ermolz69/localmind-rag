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
        var noteRepository = new KnowledgeApp.Infrastructure.Services.Persistence.NoteRepository(database.Context);
        var unitOfWork = new KnowledgeApp.Infrastructure.Services.UnitOfWork(database.Context);
        CreateNoteHandler create = new(noteRepository, unitOfWork, validator, new FakeLocalDeviceResolver());
        GetNotesHandler list = new(noteRepository);
        UpdateNoteHandler update = new(noteRepository, unitOfWork, validator);
        DeleteNoteHandler delete = new(noteRepository, unitOfWork, new FixedDateTimeProvider());

        NoteDto? note = (await create.HandleAsync(new CreateNoteRequest(Guid.Empty, null, "Draft", "Body"))).AssertSuccess();
        Contracts.Common.CursorPage<NoteDto> notes = (await list.HandleAsync(new GetNotesQuery())).AssertSuccess();
        Result updateResult = await update.HandleAsync(note.Id, new UpdateNoteRequest("Done", "Updated", null));
        Result missingUpdateResult = await update.HandleAsync(Guid.NewGuid(), new UpdateNoteRequest("Missing", "Body", null));
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
