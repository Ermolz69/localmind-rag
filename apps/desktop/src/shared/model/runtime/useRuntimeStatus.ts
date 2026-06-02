import { useCallback } from "react";
import { healthApi, runtimeApi } from "@shared/api";
import { useApiMutation, useApiQuery } from "@shared/lib/hooks";

export function useRuntimeStatus() {
  const {
    data,
    isLoading,
    error: queryError,
    reload: loadRuntimeStatus,
    setData,
  } = useApiQuery(
    async () => {
      const [health, runtime, sync] = await Promise.all([
        healthApi.getHealth(),
        runtimeApi.getRuntimeStatus(),
        runtimeApi.getSyncStatus(),
      ]);
      return { health, runtime, sync };
    },
    { fallbackError: "Unable to load runtime." },
  );

  const setupMutation = useApiMutation(
    async () => {
      const response = await runtimeApi.setupAiRuntime();
      await runtimeApi.startAiRuntime();
      return response;
    },
    { fallbackError: "Unable to install local AI runtime." },
  );

  const setupAiRuntime = useCallback(async () => {
    const response = await setupMutation.mutate();
    if (response) {
      setData((prev) => (prev ? { ...prev, runtime: response.status } : null));
      await loadRuntimeStatus();
    }
  }, [loadRuntimeStatus, setData, setupMutation]);

  return {
    health: data?.health ?? null,
    isSettingUpAi: setupMutation.isPending,
    loadRuntimeStatus,
    runtime: data?.runtime ?? null,
    runtimeError: queryError ?? setupMutation.error,
    setupAiRuntime,
    sync: data?.sync ?? null,
    isLoading,
  };
}
