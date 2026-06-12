import { useState, useCallback } from "react";
import { useBuckets } from "@features/bucket-management";
import {
  useDocumentList,
  useSemanticSearch,
  useContentSearch,
} from "@features/document-ingestion";
import type { RetrievalFilters, SearchFilterKey } from "@entities/search";
import { buildFilterChips, removeFilter } from "@entities/search";
import { extractLiveCommands } from "@shared/lib/searchFilterCommands";

export type SearchMode = "semantic" | "content";
export type ContentScope = "all" | "documents" | "notes";

export function useSemanticSearchPageViewModel() {
  const buckets = useBuckets();
  const documents = useDocumentList();
  const semanticSearch = useSemanticSearch();
  const contentSearch = useContentSearch();

  const [query, setQuery] = useState("");
  const [activeFilters, setActiveFilters] = useState<RetrievalFilters>({});
  const [searchMode, setSearchMode] = useState<SearchMode>("semantic");
  const [contentScope, setContentScope] = useState<ContentScope>("all");

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

      const nextFilters = nextDraft.filters;

      if (searchMode === "content") {
        if (nextFilters.documentId || nextFilters.fileType) {
          setContentScope("documents");
        }
      }

      setActiveFilters(nextFilters);
      setQuery(nextDraft.content);
    },
    [activeFilters, buckets.buckets, documents.documents, searchMode],
  );

  function removeActiveFilter(key: SearchFilterKey, tagKey?: string) {
    setActiveFilters((current) => removeFilter(current, key, tagKey));
  }

  async function runSearch() {
    if (searchMode === "semantic") {
      await semanticSearch.search(query, {
        filters: activeFilters,
      });
    } else {
      let includeDocuments = true;
      let includeNotes = true;

      if (contentScope === "documents") {
        includeNotes = false;
      } else if (contentScope === "notes") {
        includeDocuments = false;
      }

      await contentSearch.search(query, {
        filters: activeFilters,
        includeDocuments,
        includeNotes,
      });
    }
  }

  function clearSearch() {
    semanticSearch.clear();
    contentSearch.clear();
    setQuery("");
    setActiveFilters({});
    setContentScope("all");
  }

  const activeFilterChips = buildFilterChips(activeFilters, buckets.buckets);

  return {
    activeFilterChips,
    activeFilters,
    buckets,
    documents,
    clearSearch,
    error:
      buckets.error ??
      documents.documentListError ??
      semanticSearch.error ??
      contentSearch.error,
    isSearching:
      searchMode === "semantic"
        ? semanticSearch.isSearching
        : contentSearch.isSearching,
    query,
    semanticResults: semanticSearch.results,
    contentResults: contentSearch.results,
    removeActiveFilter,
    runSearch,
    searchSubmitted:
      searchMode === "semantic"
        ? semanticSearch.searchSubmitted
        : contentSearch.searchSubmitted,
    selectedBucketId,
    selectedBucketName:
      buckets.buckets.find((bucket) => bucket.id === selectedBucketId)?.name ??
      "All buckets",
    setQuery: handleQueryChange,
    setSelectedBucketId,
    searchMode,
    setSearchMode,
    contentScope,
    setContentScope,
  };
}
