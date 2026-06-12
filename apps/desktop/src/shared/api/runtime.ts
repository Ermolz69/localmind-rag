import type { OperationData } from "@shared/contracts";

import { request } from "./http";

export const runtimeApi = {
  getRuntimeStatus: () =>
    request<OperationData<"GetRuntimeStatus">>("/runtime/status"),

  setupAiRuntime: () =>
    request<OperationData<"SetupAiRuntime">>("/runtime/ai/setup", {
      method: "POST",
    }),

  startAiRuntime: () =>
    request<OperationData<"StartAiRuntime">>("/runtime/ai/start", {
      method: "POST",
    }),

  getSyncStatus: () => request<OperationData<"GetSyncStatus">>("/sync/status"),
};
