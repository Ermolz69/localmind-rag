#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

mod app_runtime;
mod local_api;
mod os;

use local_api::{
    commands::{
        copy_diagnostics_to_clipboard, enable_limited_mode, get_app_runtime_info, open_logs_folder,
        open_path_in_explorer, read_app_cache, restart_local_api, reveal_file_in_explorer,
        select_connected_folder, select_document_files, start_local_api_on_setup, write_app_cache,
    },
    supervisor::LocalApiSupervisor,
};
use tauri::Manager;

fn main() {
    tauri::Builder::default()
        .plugin(tauri_plugin_dialog::init())
        .plugin(tauri_plugin_clipboard_manager::init())
        .manage(LocalApiSupervisor::new())
        .invoke_handler(tauri::generate_handler![
            get_app_runtime_info,
            restart_local_api,
            enable_limited_mode,
            open_logs_folder,
            copy_diagnostics_to_clipboard,
            select_document_files,
            select_connected_folder,
            reveal_file_in_explorer,
            open_path_in_explorer,
            read_app_cache,
            write_app_cache
        ])
        .setup(|app| {
            if let Some(window) = app.get_webview_window("main") {
                window.set_icon(tauri::include_image!("./icons/128x128.png"))?;
            }
            start_local_api_on_setup(app.handle().clone());
            Ok(())
        })
        .on_window_event(|window, event| {
            if matches!(event, tauri::WindowEvent::CloseRequested { .. }) {
                let handle = window.app_handle().clone();
                handle.state::<LocalApiSupervisor>().shutdown();
            }
        })
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
