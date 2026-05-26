import type { HealthStatus } from "@entities/runtime";

import { request } from "./http";

export const healthApi = {
  getHealth: () => request<HealthStatus>("/health"),
};
