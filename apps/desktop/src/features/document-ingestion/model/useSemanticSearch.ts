import { useRef, useState } from "react";
import type { RagSource } from "@entities/source";
import { getErrorMessage, searchApi } from "@shared/api";

const DEFAULT_LIMIT = 8;

type SearchOptions = {
  bucketId?: string | null;
  limit?: number;
};

export function useSemanticSearch() {
  const requestIdRef = useRef(0);
  const [results, setResults] = useState<RagSource[]>([]);
  const [submittedQuery, setSubmittedQuery] = useState("");
  const [isSearching, setIsSearching] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function search(query: string, options: SearchOptions = {}) {
    const trimmedQuery = query.trim();

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
        bucketId: options.bucketId ?? null,
        limit: options.limit ?? DEFAULT_LIMIT,
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

  function clear() {
    requestIdRef.current += 1;
    setResults([]);
    setSubmittedQuery("");
    setIsSearching(false);
    setError(null);
  }

  return {
    clear,
    error,
    isSearching,
    results,
    search,
    searchSubmitted: Boolean(submittedQuery),
  };
}
