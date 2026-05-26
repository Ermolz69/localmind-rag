import type { DiagnosticsStatus } from "@entities/runtime";

import { request } from "./http";

export const diagnosticsApi = {
  getDiagnostics: () => request<DiagnosticsStatus>("/diagnostics"),
};
