import type { AppSettings } from "@entities/settings";

import { request } from "./http";

export const settingsApi = {
  getSettings: () => request<AppSettings>("/settings"),

  saveSettings: (settings: AppSettings) =>
    request<void>("/settings", {
      method: "PUT",
      body: JSON.stringify(settings),
    }),
};
