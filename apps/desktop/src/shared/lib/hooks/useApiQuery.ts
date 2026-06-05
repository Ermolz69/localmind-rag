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
  const [isLoading, setIsLoading] = useState(enabled && !initialData);
  const [isFetching, setIsFetching] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [rawError, setRawError] = useState<unknown | null>(null);

  // Use refs to avoid re-creating the execute function when these change
  const queryFnRef = useRef(queryFn);
  queryFnRef.current = queryFn;
  
  const fallbackErrorRef = useRef(fallbackError);
  fallbackErrorRef.current = fallbackError;

  const execute = useCallback(async () => {
    setError(null);
    setRawError(null);
    setIsFetching(true);

    try {
      const result = await queryFnRef.current();
      setData(result);
      return result;
    } catch (exception) {
      setRawError(exception);
      setError(getErrorMessage(exception, fallbackErrorRef.current));
      return null;
    } finally {
      setIsLoading(false);
      setIsFetching(false);
    }
  }, []); // Stable execute function

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
