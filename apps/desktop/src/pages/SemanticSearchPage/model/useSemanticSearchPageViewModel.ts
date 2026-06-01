import { useBuckets } from "@features/bucket-management";
import { useSemanticSearch } from "@features/document-ingestion";
import { useState } from "react";

export function useSemanticSearchPageViewModel() {
  const buckets = useBuckets();
  const semanticSearch = useSemanticSearch();

  const [query, setQuery] = useState("");
  const [selectedBucketId, setSelectedBucketId] = useState("");

  async function runSearch(nextQuery = query, nextBucketId = selectedBucketId) {
    await semanticSearch.search(nextQuery, {
      bucketId: nextBucketId || null,
    });
  }

  function clearSearch() {
    semanticSearch.clear();
    setQuery("");
    setSelectedBucketId("");
  }

  return {
    buckets,
    clearSearch,
    error: buckets.error ?? semanticSearch.error,
    isSearching: semanticSearch.isSearching,
    query,
    results: semanticSearch.results,
    runSearch,
    searchSubmitted: semanticSearch.searchSubmitted,
    selectedBucketId,
    selectedBucketName:
      buckets.buckets.find((bucket) => bucket.id === selectedBucketId)?.name ??
      "All buckets",
    setQuery,
    setSelectedBucketId,
  };
}
