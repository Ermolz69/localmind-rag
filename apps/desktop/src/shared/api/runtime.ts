import type {
  RuntimeSetupResponse,
  RuntimeStatus,
  SyncStatus,
} from "@entities/runtime";

import { request } from "./http";

export const runtimeApi = {
  getRuntimeStatus: () => request<RuntimeStatus>("/runtime/status"),

  setupAiRuntime: () =>
    request<RuntimeSetupResponse>("/runtime/ai/setup", {
      method: "POST",
    }),

  startAiRuntime: () =>
    request<void>("/runtime/ai/start", {
      method: "POST",
    }),

  getSyncStatus: () => request<SyncStatus>("/sync/status"),
};
