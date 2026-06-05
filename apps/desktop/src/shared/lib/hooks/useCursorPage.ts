import { useCallback, useEffect, useRef, useState } from "react";
import type { CursorPage } from "@shared/api";
import { getErrorMessage } from "@shared/api";

export function useCursorPage<T>(
  loadPage: (cursor?: string | null) => Promise<CursorPage<T>>,
  fallbackError = "Unable to load data.",
) {
  const [items, setItems] = useState<T[]>([]);
  const [nextCursor, setNextCursor] = useState<string | null>(null);
  const [hasMore, setHasMore] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [isLoadingMore, setIsLoadingMore] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadPageRef = useRef(loadPage);
  loadPageRef.current = loadPage;
  
  const fallbackErrorRef = useRef(fallbackError);
  fallbackErrorRef.current = fallbackError;

  const reload = useCallback(async () => {
    setError(null);
    setIsLoading(true);
    try {
      const page = await loadPageRef.current(null);
      setItems(page.items);
      setNextCursor(page.nextCursor);
      setHasMore(page.hasMore);
      return page;
    } catch (exception) {
      setError(getErrorMessage(exception, fallbackErrorRef.current));
      return null;
    } finally {
      setIsLoading(false);
    }
  }, []);

  const loadMore = useCallback(async () => {
    if (!nextCursor || isLoadingMore) {
      return null;
    }

    setError(null);
    setIsLoadingMore(true);
    try {
      const page = await loadPageRef.current(nextCursor);
      setItems((current) => [...current, ...page.items]);
      setNextCursor(page.nextCursor);
      setHasMore(page.hasMore);
      return page;
    } catch (exception) {
      setError(getErrorMessage(exception, fallbackErrorRef.current));
      return null;
    } finally {
      setIsLoadingMore(false);
    }
  }, [isLoadingMore, nextCursor]);

  useEffect(() => {
    void reload();
  }, [reload]);

  return {
    items,
    setItems,
    nextCursor,
    hasMore,
    isLoading,
    isLoadingMore,
    error,
    setError,
    reload,
    loadMore,
  };
}
