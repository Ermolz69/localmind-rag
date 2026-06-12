import type { OperationData, OperationJsonBody } from "@shared/contracts";

import { request } from "./http";

export const settingsApi = {
  getSettings: () => request<OperationData<"GetSettings">>("/settings"),

  saveSettings: (settings: OperationJsonBody<"UpdateSettings">) =>
    request<OperationData<"UpdateSettings">>("/settings", {
      method: "PUT",
      body: JSON.stringify(settings),
    }),
};
