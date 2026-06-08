import { invoke } from "@tauri-apps/api/core";

export type LocalApiStatus =
  | "NotStarted"
  | "Starting"
  | "Ready"
  | "Failed"
  | "Crashed"
  | "Restarting"
  | "Stopped";

export type AppRuntimeInfo = {
  localApiStatus: LocalApiStatus;
  baseUrl: string | null;
  pid: number | null;
  logsPath: string;
  appDataPath: string;
  lastError: string | null;
};

export function getAppRuntimeInfo() {
  if (import.meta.env.VITE_LOCAL_API_URL) {
    return Promise.resolve<AppRuntimeInfo>({
      localApiStatus: "Ready",
      baseUrl: import.meta.env.VITE_LOCAL_API_URL as string,
      pid: null,
      logsPath: "",
      appDataPath: "",
      lastError: null,
    });
  }

  return invoke<AppRuntimeInfo>("get_app_runtime_info");
}

export function restartLocalApi() {
  return invoke<AppRuntimeInfo>("restart_local_api");
}

export function openLogsFolder() {
  return invoke<void>("open_logs_folder");
}

export function copyDiagnosticsToClipboard() {
  return invoke<void>("copy_diagnostics_to_clipboard");
}
