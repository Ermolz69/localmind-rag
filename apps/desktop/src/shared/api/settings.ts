import type { AppSettings } from "@entities/settings";
import { request } from "./http";

export const settingsApi = {
  getSettings: () => request<AppSettings>("/api/settings"),
  saveSettings: (settings: AppSettings) =>
    request<void>("/api/settings", {
      method: "PUT",
      body: JSON.stringify(settings),
    }),
};
