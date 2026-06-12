import { useCallback, useRef, useState, useEffect } from "react";
import { healthApi, runtimeApi } from "@shared/api";
import { useApiQuery } from "@shared/lib/hooks";
import type { RuntimeSetupProgress } from "@entities/runtime";

export function useRuntimeStatus() {
  const {
    data,
    isLoading,
    error: queryError,
    reload: loadRuntimeStatus,
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

  const [setupProgress, setSetupProgress] =
    useState<RuntimeSetupProgress | null>(null);
  const [isSettingUpAi, setIsSettingUpAi] = useState(false);
  const [setupError, setSetupError] = useState<string | null>(null);
  const abortControllerRef = useRef<AbortController | null>(null);

  useEffect(() => {
    return () => {
      abortControllerRef.current?.abort();
    };
  }, []);

  const setupAiRuntime = useCallback(async () => {
    setIsSettingUpAi(true);
    setSetupError(null);
    setSetupProgress(null);
    abortControllerRef.current = new AbortController();

    try {
      const response = await runtimeApi.startAiRuntimeSetup();
      const setupId = response?.setupId;

      if (!setupId) throw new Error("No setup ID returned.");

      const eventResponse = await runtimeApi.watchAiRuntimeSetup(
        setupId,
        abortControllerRef.current.signal,
      );
      if (!eventResponse.body)
        throw new Error("No response body from progress stream.");

      const reader = eventResponse.body.getReader();
      const decoder = new TextDecoder("utf-8");
      let buffer = "";

      while (true) {
        const { done, value } = await reader.read();
        if (done) break;

        buffer += decoder.decode(value, { stream: true });

        const parts = buffer.split("\n\n");
        buffer = parts.pop() || "";

        for (const part of parts) {
          const dataLine = part
            .split("\n")
            .find((line) => line.startsWith("data: "));
          if (dataLine) {
            const dataStr = dataLine.replace("data: ", "").trim();
            try {
              const progress = JSON.parse(dataStr) as RuntimeSetupProgress;
              setSetupProgress(progress);

              if (progress.isFailed) {
                setSetupError(progress.message ?? "Setup failed.");
                break;
              }
              if (progress.isCompleted) {
                await runtimeApi.startAiRuntime();
                await loadRuntimeStatus();
                break;
              }
            } catch (e) {
              console.error("Failed to parse progress event", e);
            }
          }
        }
      }
    } catch (e: unknown) {
      if (e instanceof Error && e.name !== "AbortError") {
        setSetupError(e.message || "Failed to start setup.");
      } else if (!(e instanceof Error)) {
        setSetupError("Failed to start setup.");
      }
    } finally {
      setIsSettingUpAi(false);
      abortControllerRef.current = null;
    }
  }, [loadRuntimeStatus]);

  return {
    health: data?.health ?? null,
    isSettingUpAi,
    loadRuntimeStatus,
    runtime: data?.runtime ?? null,
    runtimeError: queryError ?? setupError,
    setupAiRuntime,
    setupProgress,
    sync: data?.sync ?? null,
    isLoading,
  };
}
