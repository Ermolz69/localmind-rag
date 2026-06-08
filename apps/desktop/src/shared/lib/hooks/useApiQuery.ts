import { useCallback, useEffect, useRef, useState } from "react";
import { getErrorMessage } from "@shared/api";

export interface ApiQueryOptions<T> {
  initialData?: T;
  fallbackError?: string;
  enabled?: boolean;
}

export function useApiQuery<T>(
  queryFn: () => Promise<T>,
  options: ApiQueryOptions<T> = {},
) {
  const {
    initialData = null,
    fallbackError = "Unable to load data.",
    enabled = true,
  } = options;

  const [data, setData] = useState<T | null>(initialData as T | null);
  const [isLoading, setIsLoading] = useState(enabled);
  const [isFetching, setIsFetching] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [rawError, setRawError] = useState<unknown | null>(null);
  const queryFnRef = useRef(queryFn);
  const fallbackErrorRef = useRef(fallbackError);
  const hasDataRef = useRef(initialData !== null && initialData !== undefined);
  const inFlightRef = useRef<Promise<T | null> | null>(null);

  useEffect(() => {
    queryFnRef.current = queryFn;
    fallbackErrorRef.current = fallbackError;
  }, [fallbackError, queryFn]);

  const execute = useCallback(() => {
    if (inFlightRef.current) {
      return inFlightRef.current;
    }

    const request = (async () => {
      setError(null);
      setRawError(null);
      setIsFetching(true);

      if (!hasDataRef.current) {
        setIsLoading(true);
      }

      try {
        const result = await queryFnRef.current();
        hasDataRef.current = true;
        setData(result);
        return result;
      } catch (exception) {
        setRawError(exception);
        setError(getErrorMessage(exception, fallbackErrorRef.current));
        return null;
      } finally {
        setIsLoading(false);
        setIsFetching(false);
        inFlightRef.current = null;
      }
    })();

    inFlightRef.current = request;
    return request;
  }, []);

  useEffect(() => {
    if (enabled) {
      void execute();
    }
  }, [enabled, execute]);

  return {
    data,
    setData,
    isLoading,
    isFetching,
    error,
    rawError,
    setError,
    execute,
    reload: execute,
  };
}
