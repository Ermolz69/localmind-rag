import { useCallback, useState } from "react";
import type { NoteDto } from "@entities/note";
import { bucketsApi, notesApi } from "@shared/api";
import {
  useApiQuery,
  useCursorPage,
  useDebouncedValue,
} from "@shared/lib/hooks";

export function useNoteList() {
  const [selectedBucketId, setSelectedBucketId] = useState("");
  const [query, setQuery] = useState("");
  const debouncedQuery = useDebouncedValue(query, 250);
  const [selectedNoteId, setSelectedNoteId] = useState<string | null>(null);

  const {
    data: buckets,
    isLoading: isLoadingBuckets,
    error: bucketsError,
  } = useApiQuery(() => bucketsApi.getBuckets(), {
    initialData: [],
    fallbackError: "Unable to load buckets.",
  });

  const loadNotesPage = useCallback(
    (cursor?: string | null) =>
      notesApi.getNotes({
        bucketId: selectedBucketId || undefined,
        query: debouncedQuery || undefined,
        cursor: cursor ?? undefined,
        limit: 30,
      }),
    [debouncedQuery, selectedBucketId],
  );

  const notesPage = useCursorPage<NoteDto>(
    loadNotesPage,
    "Unable to load notes.",
    `${selectedBucketId}:${debouncedQuery}`,
  );

  const selectedNote =
    notesPage.items.find((note) => note.id === selectedNoteId) ?? null;

  return {
    buckets: buckets ?? [],
    hasMore: notesPage.hasMore,
    isLoading: notesPage.isLoading || isLoadingBuckets,
    isLoadingMore: notesPage.isLoadingMore,
    loadMore: notesPage.loadMore,
    noteListError: bucketsError ?? notesPage.error,
    notes: notesPage.items,
    query,
    selectedBucketId,
    selectedNote,
    selectedNoteId,
    setNotes: notesPage.setItems,
    setQuery,
    setSelectedBucketId,
    setSelectedNoteId,
  };
}
