import type { OperationData } from "@shared/contracts";

import { request } from "./http";

export const healthApi = {
  getHealth: () => request<OperationData<"Health">>("/health"),
};
