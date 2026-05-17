import { useCallback, useEffect, useState } from "react";
import { getErrorMessage } from "@shared/api";

export function useAsyncData<T>(
  load: () => Promise<T>,
  fallbackError = "Unable to load data.",
) {
  const [data, setData] = useState<T | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const reload = useCallback(async () => {
    setError(null);
    setIsLoading(true);
    try {
      const nextData = await load();
      setData(nextData);
      return nextData;
    } catch (exception) {
      setError(getErrorMessage(exception, fallbackError));
      return null;
    } finally {
      setIsLoading(false);
    }
  }, [fallbackError, load]);

  useEffect(() => {
    void reload();
  }, [reload]);

  return { data, setData, isLoading, error, setError, reload };
}
