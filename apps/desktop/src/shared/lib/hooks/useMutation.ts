import { useCallback, useState } from "react";
import { getErrorMessage } from "@shared/api";

export function useMutation<TArgs extends unknown[], TResult>(
  mutate: (...args: TArgs) => Promise<TResult>,
  fallbackError = "Operation failed.",
) {
  const [isPending, setIsPending] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const run = useCallback(
    async (...args: TArgs) => {
      setError(null);
      setIsPending(true);
      try {
        return await mutate(...args);
      } catch (exception) {
        setError(getErrorMessage(exception, fallbackError));
        return null;
      } finally {
        setIsPending(false);
      }
    },
    [fallbackError, mutate],
  );

  return { run, isPending, error, setError };
}
