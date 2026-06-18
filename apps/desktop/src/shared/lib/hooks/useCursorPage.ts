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
  const fallbackErrorRef = useRef(fallbackError);
  const loadPageRef = useRef(loadPage);
  const reloadInFlightRef = useRef<Promise<CursorPage<T> | null> | null>(null);
  const loadMoreInFlightRef = useRef<Promise<CursorPage<T> | null> | null>(
    null,
  );

  const requestIdRef = useRef(0);

  const reload = useCallback(() => {
    if (reloadInFlightRef.current) {
      return reloadInFlightRef.current;
    }

    const id = ++requestIdRef.current;
    const request = (async () => {
      setError(null);
      setIsLoading(true);
      try {
        const page = await loadPageRef.current(null);
        if (id === requestIdRef.current) {
          setItems(page.items);
          setNextCursor(page.nextCursor);
          setHasMore(page.hasMore);
        }
        return page;
      } catch (exception) {
        if (id === requestIdRef.current) {
          setError(getErrorMessage(exception, fallbackErrorRef.current));
        }
        return null;
      } finally {
        if (id === requestIdRef.current) {
          setIsLoading(false);
          reloadInFlightRef.current = null;
        }
      }
    })();

    reloadInFlightRef.current = request;
    return request;
  }, []);

  useEffect(() => {
    fallbackErrorRef.current = fallbackError;
  }, [fallbackError]);

  useEffect(() => {
    loadPageRef.current = loadPage;
  }, [loadPage]);

  const loadMore = useCallback(() => {
    if (!nextCursor) {
      return null;
    }

    if (loadMoreInFlightRef.current) {
      return loadMoreInFlightRef.current;
    }

    const id = ++requestIdRef.current;
    const cursor = nextCursor;
    const request = (async () => {
      setError(null);
      setIsLoadingMore(true);
      try {
        const page = await loadPageRef.current(cursor);
        if (id === requestIdRef.current) {
          setItems((current) => [...current, ...page.items]);
          setNextCursor(page.nextCursor);
          setHasMore(page.hasMore);
        }
        return page;
      } catch (exception) {
        if (id === requestIdRef.current) {
          setError(getErrorMessage(exception, fallbackErrorRef.current));
        }
        return null;
      } finally {
        if (id === requestIdRef.current) {
          setIsLoadingMore(false);
          loadMoreInFlightRef.current = null;
        }
      }
    })();

    loadMoreInFlightRef.current = request;
    return request;
  }, [nextCursor]);

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
