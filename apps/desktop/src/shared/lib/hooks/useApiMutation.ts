import { useCallback, useState } from "react";
import { getErrorMessage } from "@shared/api";

export interface ApiMutationOptions {
  fallbackError?: string;
}

export function useApiMutation<TArgs extends unknown[], TResult>(
  mutationFn: (...args: TArgs) => Promise<TResult>,
  options: ApiMutationOptions = {},
) {
  const { fallbackError = "Operation failed." } = options;

  const [isPending, setIsPending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [rawError, setRawError] = useState<unknown | null>(null);

  const execute = useCallback(
    async (...args: TArgs) => {
      setError(null);
      setRawError(null);
      setIsPending(true);

      try {
        return await mutationFn(...args);
      } catch (exception) {
        setRawError(exception);
        setError(getErrorMessage(exception, fallbackError));
        return null;
      } finally {
        setIsPending(false);
      }
    },
    [fallbackError, mutationFn],
  );

  const reset = useCallback(() => {
    setError(null);
    setRawError(null);
    setIsPending(false);
  }, []);

  return {
    execute,
    mutate: execute,
    isPending,
    error,
    rawError,
    setError,
    reset,
  };
}
