import type { RuntimeStatus, SyncStatus } from "@entities/runtime";
import { request } from "./http";

export const runtimeApi = {
  getRuntimeStatus: () => request<RuntimeStatus>("/api/runtime/status"),
  getSyncStatus: () => request<SyncStatus>("/api/sync/status"),
};
