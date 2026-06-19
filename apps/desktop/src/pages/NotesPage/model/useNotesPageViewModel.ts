import { useState, useEffect } from "react";
import { confirm } from "@tauri-apps/plugin-dialog";
import { useNoteEditor, useNoteTabs } from "@features/note-editor";
import type { EditorViewMode } from "@features/note-editor";
import { useVaultExplorer } from "@features/vault-explorer";

export function useNotesPageViewModel() {
  const explorer = useVaultExplorer();
  const [editorViewMode, setEditorViewMode] =
    useState<EditorViewMode>("source");
  const [isPropertiesOpen, setIsPropertiesOpen] = useState(false);
  const [deleteTargetId, setDeleteTargetId] = useState<string | null>(null);

  const tabs = useNoteTabs({
    onConfirmCloseDirtyTab: async (noteId, title) => {
      return await confirm(`Discard unsaved changes in "${title}"?`, {
        title: "Unsaved Changes",
        kind: "warning",
      });
    },
    onConfirmReplaceDirtyTab: async (noteId, title) => {
      return await confirm(`Discard unsaved changes in "${title}"?`, {
        title: "Unsaved Changes",
        kind: "warning",
      });
    },
  });

  const editor = useNoteEditor({
    notes: explorer.notes,
    activeNoteId: tabs.activeTabId,
    setTabDirty: tabs.setTabDirty,
    updateTabTitle: tabs.updateTabTitle,
    onCreated: (note) => {
      explorer.addNote(note);
      explorer.selectNote(note.id);
      void tabs.openTab(note);
    },
    onUpdated: (note) => {
      explorer.updateNote(note);
    },
  });

  // Sync explorer selection with active tab
  useEffect(() => {
    if (tabs.activeTabId !== explorer.selectedNoteId) {
      explorer.selectNote(tabs.activeTabId);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [tabs.activeTabId]);

  useEffect(() => {
    if (!explorer.tree) return;

    const noteIds = new Set(explorer.notes.map((note) => note.id));
    for (const tab of tabs.openTabs) {
      if (!noteIds.has(tab.noteId)) {
        tabs.forceCloseTab(tab.noteId);
      }
    }
  }, [explorer.notes, explorer.tree, tabs]);

  const deleteNote = async () => {
    if (deleteTargetId) {
      const wasDeleted = await explorer.deleteNote(deleteTargetId);
      if (wasDeleted) {
        tabs.forceCloseTab(deleteTargetId);
        setDeleteTargetId(null);
      }
    }
  };

  return {
    explorer,
    tabs,
    ...editor,
    editorViewMode,
    setEditorViewMode,
    isPropertiesOpen,
    setIsPropertiesOpen,
    deleteTargetId,
    setDeleteTargetId,
    deleteNote,
    error: explorer.error ?? editor.noteEditorError,
  };
}
