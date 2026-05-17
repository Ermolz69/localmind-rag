import { useEffect, useMemo, useState } from "react";
import type { NoteDto } from "@entities/note";
import { getErrorMessage, notesApi } from "@shared/api";
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
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [noteEditorError, setNoteEditorError] = useState<string | null>(null);

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

    setNoteEditorError(null);
    setIsSubmitting(true);
    try {
      const note = await notesApi.createNote({
        title,
        markdown: createDraft.markdown,
        bucketId: createDraft.bucketId,
      });
      onCreated(note);
      setCreateDraft(emptyDraft);
      setIsCreateOpen(false);
    } catch (exception) {
      setNoteEditorError(getErrorMessage(exception, "Failed to create note."));
    } finally {
      setIsSubmitting(false);
    }
  }

  async function saveNote() {
    if (!selectedNote) {
      return;
    }

    setNoteEditorError(null);
    setIsSubmitting(true);
    try {
      await notesApi.updateNote(selectedNote.id, draft);
      onUpdated({
        ...selectedNote,
        title: draft.title,
        markdown: draft.markdown,
        bucketId: draft.bucketId,
      });
    } catch (exception) {
      setNoteEditorError(getErrorMessage(exception, "Failed to save note."));
    } finally {
      setIsSubmitting(false);
    }
  }

  async function deleteNote() {
    if (!deleteTargetId) {
      return;
    }

    setNoteEditorError(null);
    setIsSubmitting(true);
    try {
      await notesApi.deleteNote(deleteTargetId);
      onDeleted(deleteTargetId);
      setDeleteTargetId(null);
    } catch (exception) {
      setNoteEditorError(getErrorMessage(exception, "Failed to delete note."));
    } finally {
      setIsSubmitting(false);
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
    isSubmitting,
    noteEditorError,
    saveNote,
    setCreateDraft,
    setDeleteTargetId,
    setDraft,
    setIsCreateOpen,
  };
}
