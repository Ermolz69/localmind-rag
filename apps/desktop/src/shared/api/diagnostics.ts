import type { OperationData } from "@shared/contracts";

import { request } from "./http";

export const diagnosticsApi = {
  getDiagnostics: () => request<OperationData<"Diagnostics">>("/diagnostics"),
};
