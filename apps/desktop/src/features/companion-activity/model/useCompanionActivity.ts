import { useCallback, useEffect, useRef, useState } from "react";

import type { OperationData } from "@shared/contracts";
import { companionApi, getErrorMessage } from "@shared/api";

export type CompanionActivityEvent =
  OperationData<"GetCompanionActivity">["events"][number];

const POLL_INTERVAL_MS = 4000;

/**
 * Polls the companion activity feed so the phone sees what LocalMind is doing in
 * near real time (ingestion progress, watched-folder finds, device changes).
 */
export function useCompanionActivity() {
  const [events, setEvents] = useState<CompanionActivityEvent[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    try {
      const response = await companionApi.getActivity();
      setEvents(response.events);
      setError(null);
    } catch (exception) {
      setError(getErrorMessage(exception, "Unable to load activity."));
    } finally {
      setIsLoading(false);
    }
  }, []);

  const loadRef = useRef(load);
  loadRef.current = load;

  useEffect(() => {
    let cancelled = false;

    void loadRef.current();
    const interval = window.setInterval(() => {
      if (!cancelled) {
        void loadRef.current();
      }
    }, POLL_INTERVAL_MS);

    return () => {
      cancelled = true;
      window.clearInterval(interval);
    };
  }, []);

  return { events, isLoading, error };
}
