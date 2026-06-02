import { useCallback, useEffect, useState } from "react";
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

  const execute = useCallback(async () => {
    setError(null);
    setRawError(null);
    setIsFetching(true);

    if (!data) {
      setIsLoading(true);
    }

    try {
      const result = await queryFn();
      setData(result);
      return result;
    } catch (exception) {
      setRawError(exception);
      setError(getErrorMessage(exception, fallbackError));
      return null;
    } finally {
      setIsLoading(false);
      setIsFetching(false);
    }
  }, [data, fallbackError, queryFn]);

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
