import { useCallback, useEffect, useState } from "react";
import type {
  HealthStatus,
  RuntimeStatus,
  SyncStatus,
} from "@entities/runtime";
import { getErrorMessage, healthApi, runtimeApi } from "@shared/api";

export function useRuntimeStatus() {
  const [health, setHealth] = useState<HealthStatus | null>(null);
  const [runtime, setRuntime] = useState<RuntimeStatus | null>(null);
  const [sync, setSync] = useState<SyncStatus | null>(null);
  const [runtimeError, setRuntimeError] = useState<string | null>(null);

  const loadRuntimeStatus = useCallback(async () => {
    setRuntimeError(null);
    try {
      const [nextHealth, nextRuntime, nextSync] = await Promise.all([
        healthApi.getHealth(),
        runtimeApi.getRuntimeStatus(),
        runtimeApi.getSyncStatus(),
      ]);
      setHealth(nextHealth);
      setRuntime(nextRuntime);
      setSync(nextSync);
    } catch (exception) {
      setRuntimeError(getErrorMessage(exception, "Unable to load runtime."));
    }
  }, []);

  useEffect(() => {
    void loadRuntimeStatus();
  }, [loadRuntimeStatus]);

  return {
    health,
    loadRuntimeStatus,
    runtime,
    runtimeError,
    sync,
  };
}
