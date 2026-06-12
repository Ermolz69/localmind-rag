import { useRef, useState } from "react";
import type { RagSource } from "@entities/source";
import { searchApi } from "@shared/api";
import { useApiMutation } from "@shared/lib/hooks";

import type { RetrievalFilters } from "@entities/search";

const DEFAULT_LIMIT = 8;

type SearchOptions = {
  filters?: RetrievalFilters;
  limit?: number;
};

export function useSemanticSearch() {
  const requestIdRef = useRef(0);
  const [results, setResults] = useState<RagSource[]>([]);
  const [submittedQuery, setSubmittedQuery] = useState("");

  const searchMutation = useApiMutation(
    (query: string, options: SearchOptions = {}) =>
      searchApi.semanticSearch(query, {
        bucketId: options.filters?.bucketId ?? null,
        documentId: options.filters?.documentId ?? null,
        tags: options.filters?.tags ?? null,
        dateFrom: options.filters?.dateFrom ?? null,
        dateTo: options.filters?.dateTo ?? null,
        fileType: options.filters?.fileType ?? null,
        limit: options.limit ?? DEFAULT_LIMIT,
      }),
    { fallbackError: "Unable to run semantic search." },
  );

  async function search(query: string, options: SearchOptions = {}) {
    const trimmedQuery = query.trim();

    if (!trimmedQuery) {
      requestIdRef.current += 1;
      setSubmittedQuery("");
      setResults([]);
      searchMutation.reset();
      return;
    }

    const requestId = requestIdRef.current + 1;
    requestIdRef.current = requestId;

    setSubmittedQuery(trimmedQuery);

    const sources = await searchMutation.mutate(trimmedQuery, options);

    if (requestId !== requestIdRef.current) {
      return;
    }

    if (sources) {
      setResults([...sources].sort((left, right) => right.score - left.score));
    } else {
      setResults([]);
    }
  }

  function clear() {
    requestIdRef.current += 1;
    setResults([]);
    setSubmittedQuery("");
    searchMutation.reset();
  }

  return {
    clear,
    error: searchMutation.error,
    isSearching: searchMutation.isPending,
    results,
    search,
    searchSubmitted: Boolean(submittedQuery),
  };
}
