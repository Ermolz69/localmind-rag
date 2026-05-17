import { useCallback, useEffect, useState } from "react";
import type { BucketDto } from "@entities/bucket";
import type { NoteDto } from "@entities/note";
import { bucketsApi, getErrorMessage, notesApi } from "@shared/api";
import { useCursorPage, useDebouncedValue } from "@shared/lib/hooks";

export function useNoteList() {
  const [buckets, setBuckets] = useState<BucketDto[]>([]);
  const [selectedBucketId, setSelectedBucketId] = useState("");
  const [query, setQuery] = useState("");
  const debouncedQuery = useDebouncedValue(query, 250);
  const [selectedNoteId, setSelectedNoteId] = useState<string | null>(null);
  const [noteListError, setNoteListError] = useState<string | null>(null);

  const loadNotesPage = useCallback(
    (cursor?: string | null) =>
      notesApi.getNotes({
        bucketId: selectedBucketId || null,
        query: debouncedQuery || null,
        cursor,
        limit: 30,
      }),
    [debouncedQuery, selectedBucketId],
  );

  const notesPage = useCursorPage<NoteDto>(
    loadNotesPage,
    "Unable to load notes.",
  );

  const selectedNote =
    notesPage.items.find((note) => note.id === selectedNoteId) ?? null;

  useEffect(() => {
    async function loadBuckets() {
      try {
        setBuckets(await bucketsApi.getBuckets());
      } catch (exception) {
        setNoteListError(getErrorMessage(exception, "Unable to load buckets."));
      }
    }

    void loadBuckets();
  }, []);

  return {
    buckets,
    hasMore: notesPage.hasMore,
    isLoading: notesPage.isLoading,
    isLoadingMore: notesPage.isLoadingMore,
    loadMore: notesPage.loadMore,
    noteListError: noteListError ?? notesPage.error,
    notes: notesPage.items,
    query,
    selectedBucketId,
    selectedNote,
    selectedNoteId,
    setNoteListError,
    setNotes: notesPage.setItems,
    setQuery,
    setSelectedBucketId,
    setSelectedNoteId,
  };
}
