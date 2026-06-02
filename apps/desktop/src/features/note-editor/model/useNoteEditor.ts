import { useEffect, useMemo, useState } from "react";
import type { NoteDto } from "@entities/note";
import { notesApi } from "@shared/api";
import { useApiMutation } from "@shared/lib/hooks";
import type { NoteDraft } from "./types";

const emptyDraft: NoteDraft = {
  title: "",
  markdown: "",
  bucketId: null,
};

type UseNoteEditorOptions = {
  onCreated: (note: NoteDto) => void;
  onDeleted: (noteId: string) => void;
  onUpdated: (note: NoteDto) => void;
  selectedNote: NoteDto | null;
};

export function useNoteEditor({
  onCreated,
  onDeleted,
  onUpdated,
  selectedNote,
}: UseNoteEditorOptions) {
  const [draft, setDraft] = useState<NoteDraft>(emptyDraft);
  const [createDraft, setCreateDraft] = useState<NoteDraft>(emptyDraft);
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [deleteTargetId, setDeleteTargetId] = useState<string | null>(null);

  const createMutation = useApiMutation(
    (nextDraft: NoteDraft) => notesApi.createNote(nextDraft),
    { fallbackError: "Failed to create note." },
  );

  const updateMutation = useApiMutation(
    (id: string, nextDraft: NoteDraft) => notesApi.updateNote(id, nextDraft),
    { fallbackError: "Failed to save note." },
  );

  const deleteMutation = useApiMutation((id: string) => notesApi.deleteNote(id), {
    fallbackError: "Failed to delete note.",
  });

  const isDirty = useMemo(() => {
    if (!selectedNote) {
      return false;
    }

    return (
      draft.title !== selectedNote.title ||
      draft.markdown !== selectedNote.markdown ||
      draft.bucketId !== selectedNote.bucketId
    );
  }, [draft, selectedNote]);

  useEffect(() => {
    if (selectedNote) {
      setDraft({
        title: selectedNote.title,
        markdown: selectedNote.markdown,
        bucketId: selectedNote.bucketId,
      });
    } else {
      setDraft(emptyDraft);
    }
  }, [selectedNote]);

  function cancelEdit() {
    if (!selectedNote) {
      return;
    }

    setDraft({
      title: selectedNote.title,
      markdown: selectedNote.markdown,
      bucketId: selectedNote.bucketId,
    });
  }

  async function createNote() {
    const title = createDraft.title.trim();
    if (!title) {
      return;
    }

    const note = await createMutation.mutate({
      title,
      markdown: createDraft.markdown,
      bucketId: createDraft.bucketId,
    });

    if (note) {
      onCreated(note);
      setCreateDraft(emptyDraft);
      setIsCreateOpen(false);
    }
  }

  async function saveNote() {
    if (!selectedNote) {
      return;
    }

    const success = await updateMutation.mutate(selectedNote.id, draft);
    if (success !== null) {
      onUpdated({
        ...selectedNote,
        title: draft.title,
        markdown: draft.markdown,
        bucketId: draft.bucketId,
      });
    }
  }

  async function deleteNote() {
    if (!deleteTargetId) {
      return;
    }

    const success = await deleteMutation.mutate(deleteTargetId);
    if (success !== null) {
      onDeleted(deleteTargetId);
      setDeleteTargetId(null);
    }
  }

  return {
    cancelEdit,
    createDraft,
    createNote,
    deleteNote,
    deleteTargetId,
    draft,
    isCreateOpen,
    isDirty,
    isSubmitting: createMutation.isPending || updateMutation.isPending || deleteMutation.isPending,
    noteEditorError: createMutation.error ?? updateMutation.error ?? deleteMutation.error,
    saveNote,
    setCreateDraft,
    setDeleteTargetId,
    setDraft,
    setIsCreateOpen,
  };
}
