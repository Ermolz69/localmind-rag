#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

mod app_runtime;
mod local_api;
mod os;

use local_api::{
    commands::{
        copy_diagnostics_to_clipboard, get_app_runtime_info, open_logs_folder, restart_local_api,
        reveal_file_in_explorer, select_connected_folder, select_document_files,
        start_local_api_on_setup,
    },
    supervisor::LocalApiSupervisor,
};
use tauri::Manager;

fn main() {
    tauri::Builder::default()
        .manage(LocalApiSupervisor::new())
        .invoke_handler(tauri::generate_handler![
            get_app_runtime_info,
            restart_local_api,
            open_logs_folder,
            copy_diagnostics_to_clipboard,
            select_document_files,
            select_connected_folder,
            reveal_file_in_explorer
        ])
        .setup(|app| {
            start_local_api_on_setup(app.handle().clone());
            Ok(())
        })
        .on_window_event(|window, event| {
            if matches!(event, tauri::WindowEvent::CloseRequested { .. }) {
                let handle = window.app_handle().clone();
                handle.state::<LocalApiSupervisor>().stop(&handle);
            }
        })
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
