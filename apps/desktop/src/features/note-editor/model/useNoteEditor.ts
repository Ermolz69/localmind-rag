import { useCallback, useEffect, useMemo, useState } from "react";
import type { NoteDto } from "@entities/note";
import { notesApi } from "@shared/api";
import { useApiMutation } from "@shared/lib/hooks";
import type { NoteDraft } from "./types";

const emptyDraft: NoteDraft = {
  title: "",
  markdown: "",
  bucketId: null,
  folderId: null,
};

function toNoteDraft(note: NoteDto): NoteDraft {
  return {
    title: note.title,
    markdown: note.markdown,
    bucketId: note.bucketId,
    folderId: note.folderId,
  };
}

type UseNoteEditorOptions = {
  onCreated: (note: NoteDto) => void;
  onUpdated: (note: NoteDto) => void;
  notes: NoteDto[];
  activeNoteId: string | null;
  setTabDirty: (noteId: string, isDirty: boolean) => void;
  updateTabTitle: (noteId: string, title: string) => void;
};

export function useNoteEditor({
  onCreated,
  onUpdated,
  notes,
  activeNoteId,
  setTabDirty,
  updateTabTitle,
}: UseNoteEditorOptions) {
  const [drafts, setDrafts] = useState<Record<string, NoteDraft>>({});
  const [createDraft, setCreateDraft] = useState<NoteDraft>(emptyDraft);
  const [isCreateOpen, setIsCreateOpen] = useState(false);

  const createMutation = useApiMutation(
    (nextDraft: NoteDraft) =>
      notesApi.createNote({
        title: nextDraft.title,
        markdown: nextDraft.markdown,
        bucketId: nextDraft.bucketId as string,
        folderId: nextDraft.folderId,
      }),
    { fallbackError: "Failed to create note." },
  );

  const updateMutation = useApiMutation(
    (payload: { id: string; draft: NoteDraft }) =>
      notesApi.updateNote(payload.id, {
        title: payload.draft.title,
        markdown: payload.draft.markdown,
        folderId: payload.draft.folderId,
      }),
    { fallbackError: "Failed to save note." },
  );

  // Initialize draft when a new note becomes active
  useEffect(() => {
    if (!activeNoteId) return;
    setDrafts((prev) => {
      if (prev[activeNoteId]) return prev;
      const note = notes.find((n) => n.id === activeNoteId);
      if (!note) return prev;
      return {
        ...prev,
        [activeNoteId]: toNoteDraft(note),
      };
    });
  }, [activeNoteId, notes]);

  // Clean up drafts for deleted notes and sync non-dirty drafts on external changes
  useEffect(() => {
    setDrafts((prev) => {
      const next = { ...prev };
      let changed = false;

      // Remove deleted
      for (const id of Object.keys(next)) {
        if (!notes.find((n) => n.id === id)) {
          delete next[id];
          changed = true;
        }
      }

      // Sync non-dirty drafts if note changed externally
      for (const note of notes) {
        const draft = next[note.id];
        if (draft) {
          const isDirty =
            draft.title !== note.title ||
            draft.markdown !== note.markdown ||
            draft.folderId !== note.folderId;
          if (!isDirty && draft.bucketId !== note.bucketId) {
            next[note.id] = toNoteDraft(note);
            changed = true;
          }
        }
      }

      return changed ? next : prev;
    });
  }, [notes]);

  const activeNote = useMemo(() => {
    return notes.find((n) => n.id === activeNoteId) ?? null;
  }, [activeNoteId, notes]);

  const activeDraft = activeNoteId
    ? (drafts[activeNoteId] ??
      (activeNote ? toNoteDraft(activeNote) : emptyDraft))
    : emptyDraft;

  const isDirty = useMemo(() => {
    if (!activeNote) return false;
    return (
      activeDraft.title !== activeNote.title ||
      activeDraft.markdown !== activeNote.markdown ||
      activeDraft.folderId !== activeNote.folderId
    );
  }, [activeDraft, activeNote]);

  const setDraft = useCallback(
    (draft: NoteDraft) => {
      if (!activeNoteId) return;
      setDrafts((prev) => ({ ...prev, [activeNoteId]: draft }));

      const note = notes.find((n) => n.id === activeNoteId);
      if (note) {
        const isCurrentlyDirty =
          draft.title !== note.title ||
          draft.markdown !== note.markdown ||
          draft.folderId !== note.folderId;
        setTabDirty(activeNoteId, isCurrentlyDirty);
        updateTabTitle(activeNoteId, draft.title);
      }
    },
    [activeNoteId, notes, setTabDirty, updateTabTitle],
  );

  const cancelEdit = useCallback(() => {
    if (!activeNote) return;
    const cleanDraft = toNoteDraft(activeNote);
    setDrafts((prev) => ({ ...prev, [activeNote.id]: cleanDraft }));
    setTabDirty(activeNote.id, false);
    updateTabTitle(activeNote.id, activeNote.title);
  }, [activeNote, setTabDirty, updateTabTitle]);

  const saveNote = useCallback(async () => {
    if (!activeNote || !isDirty) return;

    const currentDraft = drafts[activeNote.id];
    if (!currentDraft) return;

    const success = await updateMutation.mutate({
      id: activeNote.id,
      draft: currentDraft,
    });

    if (success !== null) {
      const savedNote: NoteDto = {
        ...activeNote,
        title: currentDraft.title.trim(),
        markdown: currentDraft.markdown,
        folderId: currentDraft.folderId,
      };

      setDrafts((prev) => ({
        ...prev,
        [savedNote.id]: toNoteDraft(savedNote),
      }));
      onUpdated(savedNote);
      setTabDirty(savedNote.id, false);
      updateTabTitle(savedNote.id, savedNote.title);
    }
  }, [
    activeNote,
    isDirty,
    drafts,
    updateMutation,
    onUpdated,
    setTabDirty,
    updateTabTitle,
  ]);

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if ((e.ctrlKey || e.metaKey) && e.key === "s") {
        e.preventDefault();
        void saveNote();
      }
    };
    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [saveNote]);

  async function createNote() {
    const title = createDraft.title.trim();
    if (!title) return;

    const note = await createMutation.mutate({
      title,
      markdown: createDraft.markdown,
      bucketId: createDraft.bucketId as string,
      folderId: createDraft.folderId,
    });

    if (note) {
      onCreated(note);
      setCreateDraft(emptyDraft);
      setIsCreateOpen(false);
    }
  }

  return {
    activeDraft,
    cancelEdit,
    createDraft,
    createNote,
    isCreateOpen,
    isDirty,
    isSubmitting: createMutation.isPending || updateMutation.isPending,
    noteEditorError: createMutation.error ?? updateMutation.error,
    saveNote,
    setCreateDraft,
    setDraft,
    setIsCreateOpen,
  };
}
