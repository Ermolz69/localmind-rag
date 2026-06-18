import { useCallback, useEffect, useRef, useState } from "react";
import type { NoteDto } from "@entities/note";
import type { Schema } from "@shared/contracts";
import { bucketsApi, notesApi } from "@shared/api";
import { useApiMutation, useApiQuery } from "@shared/lib/hooks";

type NoteFolderDto = Schema<"NoteFolderDto">;

export function useVaultExplorer() {
  const [selectedBucketId, setSelectedBucketId] = useState("");
  const [expandedFolders, setExpandedFolders] = useState<Set<string>>(
    new Set(),
  );
  const [selectedFolderId, setSelectedFolderId] = useState<string | null>(null);
  const [lastSelectedFolderId, setLastSelectedFolderId] = useState<
    string | null
  >(null);
  const [selectedNoteId, setSelectedNoteId] = useState<string | null>(null);
  const selectedBucketIdRef = useRef(selectedBucketId);
  selectedBucketIdRef.current = selectedBucketId;

  const {
    data: buckets,
    isLoading: isLoadingBuckets,
    error: bucketsError,
  } = useApiQuery(() => bucketsApi.getBuckets(), {
    initialData: [],
    fallbackError: "Unable to load buckets.",
  });

  useEffect(() => {
    if (buckets && buckets.length > 0 && !selectedBucketId) {
      setSelectedBucketId(buckets[0].id);
    }
  }, [buckets, selectedBucketId]);

  const fetchTree = useCallback(async () => {
    if (!selectedBucketId) {
      return null;
    }

    const requestedBucketId = selectedBucketId;
    const result = await notesApi.getNotesTree(requestedBucketId);

    if (requestedBucketId !== selectedBucketIdRef.current) {
      return selectedBucketIdRef.current
        ? notesApi.getNotesTree(selectedBucketIdRef.current)
        : null;
    }

    return result;
  }, [selectedBucketId]);

  const {
    data: tree,
    setData: setTree,
    isLoading: isLoadingTree,
    error: treeError,
    reload: refetchTree,
  } = useApiQuery(fetchTree, {
    initialData: null,
    fallbackError: "Unable to load vault tree.",
  });

  useEffect(() => {
    setSelectedFolderId(null);
    setLastSelectedFolderId(null);
    setSelectedNoteId(null);

    if (selectedBucketId) {
      void refetchTree();
    }
  }, [refetchTree, selectedBucketId]);

  const updateFolders = useCallback(
    (updater: (folders: NoteFolderDto[]) => NoteFolderDto[]) => {
      setTree((current) =>
        current ? { ...current, folders: updater(current.folders) } : current,
      );
    },
    [setTree],
  );

  const updateNotes = useCallback(
    (updater: (notes: NoteDto[]) => NoteDto[]) => {
      setTree((current) =>
        current ? { ...current, notes: updater(current.notes) } : current,
      );
    },
    [setTree],
  );

  const createFolderMutation = useApiMutation(
    (payload: {
      name: string;
      bucketId: string;
      parentFolderId: string | null;
    }) =>
      notesApi.createNoteFolder(payload.bucketId, {
        bucketId: payload.bucketId,
        name: payload.name,
        parentFolderId: payload.parentFolderId,
      }),
    { fallbackError: "Failed to create folder." },
  );

  const createFolder = useCallback(
    async (name: string, parentFolderId: string | null) => {
      const trimmedName = name.trim();
      if (!selectedBucketId || !trimmedName) {
        return null;
      }

      const result = await createFolderMutation.mutate({
        name: trimmedName,
        bucketId: selectedBucketId,
        parentFolderId,
      });

      if (result) {
        updateFolders((current) => [...current, result]);
        if (parentFolderId) {
          setExpandedFolders((prev) => {
            const next = new Set(prev);
            next.add(parentFolderId);
            return next;
          });
        }
      }

      return result;
    },
    [createFolderMutation, selectedBucketId, updateFolders],
  );

  const renameFolderMutation = useApiMutation(
    (payload: { id: string; name: string; parentFolderId: string | null }) =>
      notesApi.updateNoteFolder(payload.id, {
        name: payload.name,
        parentFolderId: payload.parentFolderId,
      }),
    { fallbackError: "Failed to rename folder." },
  );

  const renameFolder = useCallback(
    async (
      folderId: string,
      newName: string,
      parentFolderId: string | null,
    ) => {
      const trimmedName = newName.trim();
      if (!trimmedName) return;

      const result = await renameFolderMutation.mutate({
        id: folderId,
        name: trimmedName,
        parentFolderId,
      });
      if (result) {
        updateFolders((current) =>
          current.map((folder) => (folder.id === folderId ? result : folder)),
        );
      }
      return result;
    },
    [renameFolderMutation, updateFolders],
  );

  const createNoteMutation = useApiMutation(
    (payload: { title: string; bucketId: string; folderId: string | null }) =>
      notesApi.createNote({
        bucketId: payload.bucketId,
        title: payload.title,
        markdown: "",
        folderId: payload.folderId,
      }),
    { fallbackError: "Failed to create note." },
  );

  const createNote = useCallback(
    async (title: string, parentFolderId: string | null) => {
      const trimmedTitle = title.trim();
      if (!selectedBucketId || !trimmedTitle) {
        return null;
      }

      const result = await createNoteMutation.mutate({
        title: trimmedTitle,
        bucketId: selectedBucketId,
        folderId: parentFolderId,
      });

      if (result) {
        updateNotes((current) => [...current, result]);
        if (parentFolderId) {
          setExpandedFolders((prev) => {
            const next = new Set(prev);
            next.add(parentFolderId);
            return next;
          });
        }
      }

      return result;
    },
    [createNoteMutation, selectedBucketId, updateNotes],
  );

  const renameNoteMutation = useApiMutation(
    (payload: {
      id: string;
      title: string;
      markdown: string;
      tags: Record<string, string> | null;
      folderId: string | null;
    }) =>
      notesApi.updateNote(payload.id, {
        title: payload.title,
        markdown: payload.markdown,
        tags: payload.tags,
        folderId: payload.folderId,
      }),
    { fallbackError: "Failed to rename note." },
  );

  const renameNote = useCallback(
    async (note: NoteDto, newTitle: string) => {
      const trimmedTitle = newTitle.trim();
      if (!trimmedTitle || trimmedTitle === note.title) return;

      const wasUpdated = await renameNoteMutation.mutate({
        id: note.id,
        title: trimmedTitle,
        markdown: note.markdown,
        tags: note.tags ?? null,
        folderId: note.folderId,
      });
      if (wasUpdated) {
        const updatedNote = { ...note, title: trimmedTitle };
        updateNotes((current) =>
          current.map((n) => (n.id === note.id ? updatedNote : n)),
        );
        return updatedNote;
      }
      return null;
    },
    [renameNoteMutation, updateNotes],
  );

  const moveNoteMutation = useApiMutation(
    (payload: { id: string; bucketId: string; folderId: string | null }) =>
      notesApi.moveNote(payload.id, {
        bucketId: payload.bucketId,
        folderId: payload.folderId,
      }),
    { fallbackError: "Failed to move note." },
  );

  const moveFolderMutation = useApiMutation(
    (payload: {
      id: string;
      bucketId: string;
      parentFolderId: string | null;
    }) =>
      notesApi.moveNoteFolder(payload.id, {
        bucketId: payload.bucketId,
        parentFolderId: payload.parentFolderId,
      }),
    { fallbackError: "Failed to move folder." },
  );

  const moveNote = useCallback(
    async (
      noteId: string,
      targetBucketId: string,
      targetFolderId: string | null,
    ) => {
      const result = await moveNoteMutation.mutate({
        id: noteId,
        bucketId: targetBucketId,
        folderId: targetFolderId,
      });
      if (result) {
        updateNotes((current) =>
          current.map((note) => (note.id === noteId ? result : note)),
        );
        if (targetFolderId) {
          setExpandedFolders((current) => {
            const next = new Set(current);
            next.add(targetFolderId);
            return next;
          });
        }
      }
    },
    [moveNoteMutation, updateNotes],
  );

  const moveFolder = useCallback(
    async (
      folderId: string,
      targetBucketId: string,
      targetFolderId: string | null,
    ) => {
      const result = await moveFolderMutation.mutate({
        id: folderId,
        bucketId: targetBucketId,
        parentFolderId: targetFolderId,
      });
      if (result) {
        updateFolders((current) =>
          current.map((folder) => (folder.id === folderId ? result : folder)),
        );
        if (targetFolderId) {
          setExpandedFolders((current) => {
            const next = new Set(current);
            next.add(targetFolderId);
            return next;
          });
        }
      }
    },
    [moveFolderMutation, updateFolders],
  );

  const deleteNoteMutation = useApiMutation(
    (id: string) => notesApi.deleteNote(id),
    { fallbackError: "Failed to delete note." },
  );

  const deleteFolderMutation = useApiMutation(
    (id: string) => notesApi.deleteNoteFolder(id),
    { fallbackError: "Failed to delete folder." },
  );

  const deleteNote = useCallback(
    async (id: string) => {
      const res = await deleteNoteMutation.mutate(id);
      if (res !== null) {
        updateNotes((current) => current.filter((note) => note.id !== id));
        return true;
      }
      return false;
    },
    [deleteNoteMutation, updateNotes],
  );

  const deleteFolder = useCallback(
    async (id: string) => {
      const res = await deleteFolderMutation.mutate(id);
      if (res !== null) {
        updateFolders((current) =>
          current.filter((folder) => folder.id !== id),
        );
      }
    },
    [deleteFolderMutation, updateFolders],
  );

  const toggleFolder = useCallback((folderId: string) => {
    setExpandedFolders((prev) => {
      const next = new Set(prev);
      if (next.has(folderId)) {
        next.delete(folderId);
      } else {
        next.add(folderId);
      }
      return next;
    });
  }, []);

  const selectFolder = useCallback((folderId: string | null) => {
    setSelectedFolderId(folderId);
    setLastSelectedFolderId(folderId);
    setSelectedNoteId(null);
  }, []);

  const selectNote = useCallback((noteId: string | null) => {
    setSelectedNoteId(noteId);
    setSelectedFolderId(null);
  }, []);

  const addNote = useCallback(
    (note: NoteDto) => {
      updateNotes((current) => [
        note,
        ...current.filter((item) => item.id !== note.id),
      ]);
    },
    [updateNotes],
  );

  const updateNote = useCallback(
    (note: NoteDto) => {
      updateNotes((current) =>
        current.map((item) => (item.id === note.id ? note : item)),
      );
    },
    [updateNotes],
  );

  // Expose flat arrays for rendering and logic
  const folders = tree?.folders ?? [];
  const notes = tree?.notes ?? [];

  return {
    buckets: buckets ?? [],
    selectedBucketId,
    setSelectedBucketId,
    tree,
    folders,
    notes,
    isLoading: isLoadingBuckets || isLoadingTree,
    error: bucketsError || treeError,
    expandedFolders,
    toggleFolder,
    selectedFolderId,
    lastSelectedFolderId,
    selectFolder,
    selectedNoteId,
    selectNote,
    addNote,
    updateNote,
    refetchTree,
    createFolder,
    renameFolder,
    createNote,
    renameNote,
    isCreatingFolder: createFolderMutation.isPending,
    isRenamingFolder: renameFolderMutation.isPending,
    isCreatingNote: createNoteMutation.isPending,
    isRenamingNote: renameNoteMutation.isPending,
    moveNote,
    moveFolder,
    deleteNote,
    deleteFolder,
    isMovingNote: moveNoteMutation.isPending,
    isMovingFolder: moveFolderMutation.isPending,
    isDeletingNote: deleteNoteMutation.isPending,
    isDeletingFolder: deleteFolderMutation.isPending,
  };
}
