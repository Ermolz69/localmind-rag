import { useState } from "react";

import type { RagSource } from "@entities/source";
import { getErrorMessage, searchApi } from "@shared/api";

/**
 * Mobile semantic search over the already-indexed knowledge base on the
 * computer. Read-only: the phone does not manage files at this stage.
 */
export function useCompanionSearch() {
  const [query, setQuery] = useState("");
  const [results, setResults] = useState<RagSource[] | null>(null);
  const [isSearching, setIsSearching] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function runSearch() {
    const trimmed = query.trim();

    if (!trimmed || isSearching) {
      return;
    }

    setIsSearching(true);
    setError(null);

    try {
      const sources = await searchApi.semanticSearch(trimmed);
      setResults(sources);
    } catch (exception) {
      setError(getErrorMessage(exception, "Search failed."));
      setResults(null);
    } finally {
      setIsSearching(false);
    }
  }

  return { query, setQuery, results, isSearching, error, runSearch };
}
