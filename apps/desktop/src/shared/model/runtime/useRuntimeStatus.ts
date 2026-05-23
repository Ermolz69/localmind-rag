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
  const [isSettingUpAi, setIsSettingUpAi] = useState(false);

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

  const setupAiRuntime = useCallback(async () => {
    setRuntimeError(null);
    setIsSettingUpAi(true);
    try {
      const response = await runtimeApi.setupAiRuntime();
      setRuntime(response.status);
      await runtimeApi.startAiRuntime();
      await loadRuntimeStatus();
    } catch (exception) {
      setRuntimeError(
        getErrorMessage(exception, "Unable to install local AI runtime."),
      );
    } finally {
      setIsSettingUpAi(false);
    }
  }, [loadRuntimeStatus]);

  return {
    health,
    isSettingUpAi,
    loadRuntimeStatus,
    runtime,
    runtimeError,
    setupAiRuntime,
    sync,
  };
}
