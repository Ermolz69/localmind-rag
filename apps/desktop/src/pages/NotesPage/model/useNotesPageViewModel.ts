import { useNoteEditor } from "@features/note-editor";
import { useVaultExplorer } from "@features/vault-explorer";

export function useNotesPageViewModel() {
  const explorer = useVaultExplorer();

  const editor = useNoteEditor({
    onCreated: (note) => {
      explorer.refetchTree();
      explorer.selectNote(note.id);
    },
    onDeleted: (noteId) => {
      explorer.refetchTree();
      if (explorer.selectedNoteId === noteId) {
        explorer.selectNote(null);
      }
    },
    onUpdated: () => {
      explorer.refetchTree();
    },
    selectedNote:
      explorer.notes.find((n) => n.id === explorer.selectedNoteId) ?? null,
  });

  return {
    explorer,
    ...editor,
    error: explorer.error ?? editor.noteEditorError,
  };
}
