import { useRef, useState } from "react";
import type { RagSource } from "@entities/source";
import { useBuckets } from "@features/bucket-management";
import { getErrorMessage, searchApi } from "@shared/api";

const DEFAULT_LIMIT = 8;

export function useSemanticSearchPageViewModel() {
  const buckets = useBuckets();
  const requestIdRef = useRef(0);

  const [query, setQuery] = useState("");
  const [selectedBucketId, setSelectedBucketId] = useState("");
  const [results, setResults] = useState<RagSource[]>([]);
  const [submittedQuery, setSubmittedQuery] = useState("");
  const [isSearching, setIsSearching] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function runSearch(nextQuery = query, nextBucketId = selectedBucketId) {
    const trimmedQuery = nextQuery.trim();

    if (!trimmedQuery) {
      requestIdRef.current += 1;
      setSubmittedQuery("");
      setResults([]);
      setIsSearching(false);
      setError(null);
      return;
    }

    const requestId = requestIdRef.current + 1;
    requestIdRef.current = requestId;

    setSubmittedQuery(trimmedQuery);
    setIsSearching(true);
    setError(null);

    try {
      const sources = await searchApi.semanticSearch(trimmedQuery, {
        bucketId: nextBucketId || null,
        limit: DEFAULT_LIMIT,
      });

      if (requestId !== requestIdRef.current) {
        return;
      }

      setResults([...sources].sort((left, right) => right.score - left.score));
    } catch (exception) {
      if (requestId !== requestIdRef.current) {
        return;
      }

      setResults([]);
      setError(getErrorMessage(exception, "Unable to run semantic search."));
    } finally {
      if (requestId === requestIdRef.current) {
        setIsSearching(false);
      }
    }
  }

  function clearSearch() {
    requestIdRef.current += 1;
    setQuery("");
    setSelectedBucketId("");
    setSubmittedQuery("");
    setResults([]);
    setIsSearching(false);
    setError(null);
  }

  return {
    buckets,
    clearSearch,
    error: buckets.error ?? error,
    isSearching,
    query,
    results,
    runSearch,
    searchSubmitted: Boolean(submittedQuery),
    selectedBucketId,
    selectedBucketName:
      buckets.buckets.find((bucket) => bucket.id === selectedBucketId)?.name ??
      "All buckets",
    setQuery,
    setSelectedBucketId,
  };
}