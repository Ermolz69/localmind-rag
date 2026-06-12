import { useCallback, useState, useEffect } from "react";
import { bucketsApi, notesApi } from "@shared/api";
import { useApiQuery, useApiMutation } from "@shared/lib/hooks";

export function useVaultExplorer() {
  const [selectedBucketId, setSelectedBucketId] = useState("");
  const [expandedFolders, setExpandedFolders] = useState<Set<string>>(
    new Set(),
  );
  const [selectedFolderId, setSelectedFolderId] = useState<string | null>(null);
  const [selectedNoteId, setSelectedNoteId] = useState<string | null>(null);

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

  const fetchTree = useCallback(() => {
    if (!selectedBucketId) return Promise.resolve(null);
    return notesApi.getNotesTree(selectedBucketId);
  }, [selectedBucketId]);

  const {
    data: tree,
    isLoading: isLoadingTree,
    error: treeError,
    reload: refetchTree,
  } = useApiQuery(fetchTree, {
    initialData: null,
    fallbackError: "Unable to load vault tree.",
  });

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
    async (name: string) => {
      if (!selectedBucketId) return;

      const result = await createFolderMutation.mutate({
        name,
        bucketId: selectedBucketId,
        parentFolderId: selectedFolderId,
      });

      if (result) {
        refetchTree();
        // optionally expand the parent folder
        if (selectedFolderId) {
          setExpandedFolders((prev) => {
            const next = new Set(prev);
            next.add(selectedFolderId);
            return next;
          });
        }
      }
    },
    [selectedBucketId, selectedFolderId, createFolderMutation, refetchTree],
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
        refetchTree();
      }
    },
    [moveNoteMutation, refetchTree],
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
        refetchTree();
      }
    },
    [moveFolderMutation, refetchTree],
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
      if (res) refetchTree();
    },
    [deleteNoteMutation, refetchTree],
  );

  const deleteFolder = useCallback(
    async (id: string) => {
      const res = await deleteFolderMutation.mutate(id);
      if (res) refetchTree();
    },
    [deleteFolderMutation, refetchTree],
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
    setSelectedNoteId(null);
  }, []);

  const selectNote = useCallback((noteId: string | null) => {
    setSelectedNoteId(noteId);
    setSelectedFolderId(null);
  }, []);

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
    selectFolder,
    selectedNoteId,
    selectNote,
    refetchTree,
    createFolder,
    isCreatingFolder: createFolderMutation.isPending,
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
