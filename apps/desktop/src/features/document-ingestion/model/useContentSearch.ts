import { useRef, useState } from "react";
import type { ContentSearchHitDto } from "@shared/contracts";
import { searchApi } from "@shared/api";
import { useApiMutation } from "@shared/lib/hooks";
import type { RetrievalFilters } from "@entities/search";

const DEFAULT_LIMIT = 20;

type SearchOptions = {
  filters?: RetrievalFilters;
  includeDocuments?: boolean;
  includeNotes?: boolean;
  limit?: number;
};

export function useContentSearch() {
  const requestIdRef = useRef(0);
  const [results, setResults] = useState<ContentSearchHitDto[]>([]);
  const [submittedQuery, setSubmittedQuery] = useState("");

  const searchMutation = useApiMutation(
    (query: string, options: SearchOptions = {}) =>
      searchApi.contentSearch(query, {
        bucketId: options.filters?.bucketId ?? null,
        documentId: options.filters?.documentId ?? null,
        tags: options.filters?.tags ?? null,
        dateFrom: options.filters?.dateFrom ?? null,
        dateTo: options.filters?.dateTo ?? null,
        fileType: options.filters?.fileType ?? null,
        includeDocuments: options.includeDocuments ?? true,
        includeNotes: options.includeNotes ?? true,
        limit: options.limit ?? DEFAULT_LIMIT,
      }),
    { fallbackError: "Unable to run content search." },
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

    const hits = await searchMutation.mutate(trimmedQuery, options);

    if (requestId !== requestIdRef.current) {
      return;
    }

    if (hits) {
      setResults(hits); // Backend already sorts by score
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
