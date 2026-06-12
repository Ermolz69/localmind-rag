import { useState, useCallback } from "react";
import { useBuckets } from "@features/bucket-management";
import {
  useDocumentList,
  useSemanticSearch,
} from "@features/document-ingestion";
import type { RetrievalFilters, SearchFilterKey } from "@entities/search";
import { buildFilterChips, removeFilter } from "@entities/search";
import { extractLiveCommands } from "@shared/lib/searchFilterCommands";

export function useSemanticSearchPageViewModel() {
  const buckets = useBuckets();
  const documents = useDocumentList();
  const semanticSearch = useSemanticSearch();

  const [query, setQuery] = useState("");
  const [activeFilters, setActiveFilters] = useState<RetrievalFilters>({});

  const selectedBucketId = activeFilters.bucketId ?? "";

  function setSelectedBucketId(bucketId: string) {
    setActiveFilters((current) => ({
      ...current,
      bucketId: bucketId || null,
    }));
  }

  const handleQueryChange = useCallback(
    (nextValue: string) => {
      const nextDraft = extractLiveCommands(
        nextValue,
        activeFilters,
        buckets.buckets,
        documents.documents,
      );
      setActiveFilters(nextDraft.filters);
      setQuery(nextDraft.content);
    },
    [activeFilters, buckets.buckets, documents.documents],
  );

  function removeActiveFilter(key: SearchFilterKey, tagKey?: string) {
    setActiveFilters((current) => removeFilter(current, key, tagKey));
  }

  async function runSearch() {
    await semanticSearch.search(query, {
      filters: activeFilters,
    });
  }

  function clearSearch() {
    semanticSearch.clear();
    setQuery("");
    setActiveFilters({});
  }

  const activeFilterChips = buildFilterChips(activeFilters, buckets.buckets);

  return {
    activeFilterChips,
    activeFilters,
    buckets,
    documents,
    clearSearch,
    error: buckets.error ?? documents.documentListError ?? semanticSearch.error,
    isSearching: semanticSearch.isSearching,
    query,
    results: semanticSearch.results,
    removeActiveFilter,
    runSearch,
    searchSubmitted: semanticSearch.searchSubmitted,
    selectedBucketId,
    selectedBucketName:
      buckets.buckets.find((bucket) => bucket.id === selectedBucketId)?.name ??
      "All buckets",
    setQuery: handleQueryChange,
    setSelectedBucketId,
  };
}
