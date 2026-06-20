import type { OperationData } from "@shared/contracts";

import { request } from "./http";

export type LogCleanupResult = {
  deletedFiles: number;
  freedBytes: number;
  skippedFiles: number;
};

export const diagnosticsApi = {
  getDiagnostics: () => request<OperationData<"Diagnostics">>("/diagnostics"),
  cleanupLogs: () =>
    request<LogCleanupResult>("/diagnostics/logs/cleanup", { method: "POST" }),
};
