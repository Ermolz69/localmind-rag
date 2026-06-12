import type { OperationData } from "@shared/contracts";

import { getApiBaseUrl, request } from "./http";

export const runtimeApi = {
  getRuntimeStatus: () =>
    request<OperationData<"GetRuntimeStatus">>("/runtime/status"),

  startAiRuntimeSetup: () =>
    request<OperationData<"StartAiRuntimeSetup">>("/runtime/ai/setup", {
      method: "POST",
    }),

  watchAiRuntimeSetup: (setupId: string, signal?: AbortSignal) =>
    fetch(`${getApiBaseUrl()}/api/v1/runtime/ai/setup/${setupId}/events`, {
      signal,
      headers: { Accept: "text/event-stream" },
    }),

  startAiRuntime: () =>
    request<OperationData<"StartAiRuntime">>("/runtime/ai/start", {
      method: "POST",
    }),

  getSyncStatus: () => request<OperationData<"GetSyncStatus">>("/sync/status"),
};
