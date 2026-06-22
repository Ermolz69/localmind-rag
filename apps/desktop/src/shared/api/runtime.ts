import type { OperationData } from "@shared/contracts";

import { getApiBaseUrl, request } from "./http";

export const runtimeApi = {
  getRuntimeStatus: () =>
    request<OperationData<"GetRuntimeStatus">>("/runtime/status", {
      cache: "no-store",
    }),

  startAiRuntimeSetup: () =>
    request<OperationData<"StartAiRuntimeSetup">>("/runtime/ai/setup", {
      method: "POST",
    }),

  watchAiRuntimeSetup: (setupId: string, signal?: AbortSignal) =>
    fetch(`${getApiBaseUrl()}/api/v1/runtime/ai/setup/${setupId}/events`, {
      cache: "no-store",
      signal,
      headers: {
        Accept: "text/event-stream",
      },
    }),

  startAiRuntime: () =>
    request<OperationData<"StartAiRuntime">>("/runtime/ai/start", {
      method: "POST",
    }),

  getSyncStatus: () => request<OperationData<"GetSyncStatus">>("/sync/status"),
};
