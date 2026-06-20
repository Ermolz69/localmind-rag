import { invoke } from "@tauri-apps/api/core";

/**
 * Opens a native folder picker and resolves with the chosen folder path,
 * or null when the dialog is cancelled.
 */
export function pickFolder(): Promise<string | null> {
  return invoke<string | null>("select_connected_folder");
}

/** Opens the given folder in the OS file explorer. */
export function openPathInExplorer(path: string): Promise<void> {
  return invoke<void>("open_path_in_explorer", { path });
}

/** Reveals the given file in the OS file explorer (selects it in its folder). */
export function revealFileInExplorer(path: string): Promise<void> {
  return invoke<void>("reveal_file_in_explorer", { path });
}
