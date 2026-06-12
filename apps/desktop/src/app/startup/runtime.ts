import { invoke } from "@tauri-apps/api/core";

export type LocalApiStatus =
  | "NotStarted"
  | "Starting"
  | "Ready"
  | "Failed"
  | "Crashed"
  | "Restarting"
  | "Stopped";

export type DesktopMode = "Normal" | "Limited";

export type AppRuntimeInfo = {
  localApiStatus: LocalApiStatus;
  baseUrl: string | null;
  pid: number | null;
  logsPath: string;
  appDataPath: string;
  lastError: string | null;
  desktopMode: DesktopMode;
  apiAvailable: boolean;
};

export function getAppRuntimeInfo() {
  return invoke<AppRuntimeInfo>("get_app_runtime_info");
}

export async function restartLocalApi(): Promise<void> {
  await invoke("restart_local_api");
}

export function enableLimitedMode() {
  return invoke<AppRuntimeInfo>("enable_limited_mode");
}

export function openLogsFolder() {
  return invoke<void>("open_logs_folder");
}

export function copyDiagnosticsToClipboard() {
  return invoke<void>("copy_diagnostics_to_clipboard");
}

export function readAppCache(key: string) {
  return invoke<string | null>("read_app_cache", { key });
}

export function writeAppCache(key: string, payload: string) {
  return invoke<void>("write_app_cache", { key, payload });
}
