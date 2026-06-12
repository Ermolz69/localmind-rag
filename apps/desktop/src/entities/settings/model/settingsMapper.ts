import type { OperationData, OperationJsonBody } from "@shared/contracts";

import type { AppSettings } from "./types";

const defaultWatchedFolders: AppSettings["watchedFolders"] = {
  enabled: false,
  debounceMilliseconds: 1000,
  deletePolicy: "MarkDeleted",
  folders: [],
  ignoredFolders: [".git", "node_modules", "bin", "obj"],
  ignoredPatterns: ["~$*", "*.tmp", "*.bak"],
  maxFileSizeMb: 100,
  allowedExtensions: null,
  storageMode: "LinkOnly",
};

export function toAppSettings(
  settings: OperationData<"GetSettings">,
): AppSettings {
  return {
    ...settings,
    watchedFolders: settings.watchedFolders ?? {
      ...defaultWatchedFolders,
      folders: [],
      ignoredFolders: [...(defaultWatchedFolders.ignoredFolders ?? [])],
      ignoredPatterns: [...(defaultWatchedFolders.ignoredPatterns ?? [])],
    },
  };
}

export function toAppSettingsDto(
  settings: AppSettings,
): OperationJsonBody<"UpdateSettings"> {
  return settings;
}
