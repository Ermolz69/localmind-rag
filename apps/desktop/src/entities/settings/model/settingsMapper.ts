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

const defaultCompanionMode: AppSettings["companionMode"] = {
  enabled: false,
};

const defaultDiagnostics: AppSettings["diagnostics"] = {
  enabled: true,
  developerModeEnabled: false,
  minimumLogLevel: "Information",
  useSeparateLogFiles: false,
  enableErrorLogs: true,
  enableSqlLogs: false,
  enableHttpLogs: true,
  enableDiagnosticEventLogs: false,
  enableDebugTrace: false,
  logRetainedDays: 14,
};

export function toAppSettings(
  settings: OperationData<"GetSettings">,
): AppSettings {
  return {
    ...settings,
    diagnostics: settings.diagnostics ?? defaultDiagnostics,
    companionMode: settings.companionMode ?? { ...defaultCompanionMode },
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
