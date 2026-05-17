import { useNoteEditor, useNoteList } from "@features/note-editor";

export function useNotesPageViewModel() {
  const list = useNoteList();
  const editor = useNoteEditor({
    onCreated: (note) => {
      list.setNotes((current) => [note, ...current]);
      list.setSelectedNoteId(note.id);
    },
    onDeleted: (noteId) => {
      list.setNotes((current) => current.filter((note) => note.id !== noteId));
      list.setSelectedNoteId((current) =>
        current === noteId ? null : current,
      );
    },
    onUpdated: (updatedNote) => {
      list.setNotes((current) =>
        current.map((note) =>
          note.id === updatedNote.id ? updatedNote : note,
        ),
      );
    },
    selectedNote: list.selectedNote,
  });

  function selectNote(noteId: string) {
    if (editor.isDirty && !window.confirm("Discard unsaved note changes?")) {
      return;
    }

    list.setSelectedNoteId(noteId);
  }

  return {
    ...list,
    ...editor,
    error: list.noteListError ?? editor.noteEditorError,
    selectNote,
  };
}
