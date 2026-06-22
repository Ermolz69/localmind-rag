import { useCallback, useEffect, useRef, useState } from "react";

import type { RuntimeSetupProgress } from "@entities/runtime";
import { healthApi, runtimeApi } from "@shared/api";
import { useApiQuery } from "@shared/lib/hooks";

const STARTUP_RUNTIME_POLL_INTERVAL_MS = 1_000;
const STARTUP_RUNTIME_POLL_MAX_ATTEMPTS = 60;

type SetupStreamResult = "completed" | "failed" | null;

function parseSetupProgressFrame(frame: string): RuntimeSetupProgress | null {
  const data = frame
    .split(/\r?\n/)
    .filter((line) => line.startsWith("data:"))
    .map((line) => line.slice("data:".length).trimStart())
    .join("\n");

  if (!data) {
    return null;
  }

  return JSON.parse(data) as RuntimeSetupProgress;
}

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

      return {
        health,
        runtime,
        sync,
      };
    },
    {
      fallbackError: "Unable to load runtime.",
    },
  );

  const [setupProgress, setSetupProgress] =
    useState<RuntimeSetupProgress | null>(null);
  const [isSettingUpAi, setIsSettingUpAi] = useState(false);
  const [setupError, setSetupError] = useState<string | null>(null);

  const abortControllerRef = useRef<AbortController | null>(null);
  const startupPollAttemptsRef = useRef(0);
  const startupPollInFlightRef = useRef(false);

  const shouldPollRuntimeStartup =
    !isSettingUpAi &&
    data?.runtime?.modelsAvailable === true &&
    data.runtime.setupRequired === false &&
    data.runtime.aiRuntimeStatus === "Stopped";

  useEffect(() => {
    return () => {
      abortControllerRef.current?.abort();
    };
  }, []);

  useEffect(() => {
    if (!shouldPollRuntimeStartup) {
      startupPollAttemptsRef.current = 0;
      startupPollInFlightRef.current = false;

      return;
    }

    let isDisposed = false;
    let timeoutId: ReturnType<typeof setTimeout> | null = null;

    const pollRuntimeStatus = async () => {
      if (
        isDisposed ||
        startupPollInFlightRef.current ||
        startupPollAttemptsRef.current >= STARTUP_RUNTIME_POLL_MAX_ATTEMPTS
      ) {
        return;
      }

      startupPollInFlightRef.current = true;
      startupPollAttemptsRef.current += 1;

      try {
        await loadRuntimeStatus();
      } finally {
        startupPollInFlightRef.current = false;
      }

      if (
        !isDisposed &&
        startupPollAttemptsRef.current < STARTUP_RUNTIME_POLL_MAX_ATTEMPTS
      ) {
        timeoutId = setTimeout(
          () => void pollRuntimeStatus(),
          STARTUP_RUNTIME_POLL_INTERVAL_MS,
        );
      }
    };

    timeoutId = setTimeout(
      () => void pollRuntimeStatus(),
      STARTUP_RUNTIME_POLL_INTERVAL_MS,
    );

    return () => {
      isDisposed = true;

      if (timeoutId) {
        clearTimeout(timeoutId);
      }
    };
  }, [loadRuntimeStatus, shouldPollRuntimeStartup]);

  const setupAiRuntime = useCallback(async () => {
    setIsSettingUpAi(true);
    setSetupError(null);
    setSetupProgress(null);

    const abortController = new AbortController();
    abortControllerRef.current = abortController;

    try {
      const response = await runtimeApi.startAiRuntimeSetup();
      const setupId = response?.setupId;

      if (!setupId) {
        throw new Error("No setup ID returned.");
      }

      const eventResponse = await runtimeApi.watchAiRuntimeSetup(
        setupId,
        abortController.signal,
      );

      if (!eventResponse.body) {
        throw new Error("No response body from progress stream.");
      }

      const reader = eventResponse.body.getReader();
      const decoder = new TextDecoder("utf-8");

      let buffer = "";
      let completedEventReceived = false;
      let failedEventReceived = false;

      const handleProgress = (
        progress: RuntimeSetupProgress,
      ): SetupStreamResult => {
        setSetupProgress(progress);

        if (progress.isFailed) {
          setSetupError(progress.message ?? "Setup failed.");
          return "failed";
        }

        if (progress.isCompleted) {
          return "completed";
        }

        return null;
      };

      const processFrames = (frames: string[]): SetupStreamResult => {
        for (const frame of frames) {
          if (!frame.trim()) {
            continue;
          }

          try {
            const progress = parseSetupProgressFrame(frame);

            if (!progress) {
              continue;
            }

            const result = handleProgress(progress);

            if (result) {
              return result;
            }
          } catch (error) {
            console.error("Failed to parse progress event", error);
          }
        }

        return null;
      };

      while (true) {
        const { done, value } = await reader.read();

        if (value) {
          buffer += decoder.decode(value, { stream: !done });
        }

        const frames = buffer.split(/\r?\n\r?\n/);
        buffer = frames.pop() ?? "";

        const result = processFrames(frames);

        if (result === "failed") {
          failedEventReceived = true;
          break;
        }

        if (result === "completed") {
          completedEventReceived = true;
        }

        if (done) {
          break;
        }
      }

      buffer += decoder.decode();

      if (!failedEventReceived && buffer.trim()) {
        const result = processFrames([buffer]);

        if (result === "failed") {
          failedEventReceived = true;
        }

        if (result === "completed") {
          completedEventReceived = true;
        }
      }

      if (failedEventReceived) {
        return;
      }

      /*
       * The final SSE frame can occasionally be lost when the response closes.
       * The LocalApi runtime status is the source of truth, so verify it after
       * the stream ends instead of returning UI to the Install state.
       */
      if (!completedEventReceived) {
        const status = await runtimeApi.getRuntimeStatus();

        if (status.setupRequired || !status.modelsAvailable) {
          throw new Error(
            status.setupReason ??
              "AI setup ended before LocalMind confirmed the installed models.",
          );
        }
      }

      await runtimeApi.startAiRuntime();
      await loadRuntimeStatus();
    } catch (error: unknown) {
      if (error instanceof Error && error.name !== "AbortError") {
        setSetupError(error.message || "Failed to start setup.");
      } else if (!(error instanceof Error)) {
        setSetupError("Failed to start setup.");
      }
    } finally {
      setIsSettingUpAi(false);

      if (abortControllerRef.current === abortController) {
        abortControllerRef.current = null;
      }
    }
  }, [loadRuntimeStatus]);

  return {
    health: data?.health ?? null,
    isSettingUpAi,
    isWaitingForAiRuntime: shouldPollRuntimeStartup,
    loadRuntimeStatus,
    runtime: data?.runtime ?? null,
    runtimeError: queryError ?? setupError,
    setupAiRuntime,
    setupProgress,
    sync: data?.sync ?? null,
    isLoading,
  };
}
