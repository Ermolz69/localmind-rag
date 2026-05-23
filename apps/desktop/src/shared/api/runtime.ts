import type {
  RuntimeSetupResponse,
  RuntimeStatus,
  SyncStatus,
} from "@entities/runtime";
import { request } from "./http";

export const runtimeApi = {
  getRuntimeStatus: () => request<RuntimeStatus>("/api/runtime/status"),
  setupAiRuntime: () =>
    request<RuntimeSetupResponse>("/api/runtime/ai/setup", {
      method: "POST",
    }),
  startAiRuntime: () =>
    request<void>("/api/runtime/ai/start", {
      method: "POST",
    }),
  getSyncStatus: () => request<SyncStatus>("/api/sync/status"),
};
