use tauri::{AppHandle, Manager, State};

use crate::{
    app_runtime::ErrorDto,
    local_api::{paths, state::AppRuntimeInfo, supervisor::LocalApiSupervisor},
    os,
};

pub fn start_local_api_on_setup(app: AppHandle) {
    app.state::<LocalApiSupervisor>().start(app.clone(), false);
}

#[tauri::command]
pub fn get_app_runtime_info(supervisor: State<'_, LocalApiSupervisor>) -> AppRuntimeInfo {
    supervisor.runtime_info()
}

#[tauri::command]
pub fn restart_local_api(
    app: AppHandle,
    supervisor: State<'_, LocalApiSupervisor>,
) -> AppRuntimeInfo {
    supervisor.restart(app);
    supervisor.runtime_info()
}

#[tauri::command]
pub fn open_logs_folder() -> Result<(), ErrorDto> {
    let root = paths::app_root();
    paths::ensure_runtime_dirs(&root).map_err(ErrorDto::from)?;
    os::open_folder(&paths::logs_dir(&root)).map_err(ErrorDto::from)
}

#[tauri::command]
pub fn copy_diagnostics_to_clipboard(
    supervisor: State<'_, LocalApiSupervisor>,
) -> Result<(), ErrorDto> {
    let info = supervisor.runtime_info();
    let text = format!(
        "LocalMind diagnostics\nstatus: {:?}\nbaseUrl: {}\npid: {}\nlogsPath: {}\nappDataPath: {}\nlastError: {}",
        info.local_api_status,
        info.base_url.unwrap_or_default(),
        info.pid.map(|pid| pid.to_string()).unwrap_or_default(),
        info.logs_path,
        info.app_data_path,
        info.last_error.unwrap_or_default()
    );

    os::copy_text_to_clipboard(&text).map_err(ErrorDto::from)
}

#[tauri::command]
pub fn select_document_files() -> Result<Vec<String>, ErrorDto> {
    os::select_document_files().map_err(ErrorDto::from)
}

#[tauri::command]
pub fn select_connected_folder() -> Result<Option<String>, ErrorDto> {
    os::select_connected_folder().map_err(ErrorDto::from)
}

#[tauri::command]
pub fn reveal_file_in_explorer(path: String) -> Result<(), ErrorDto> {
    os::reveal_file(&path).map_err(ErrorDto::from)
}
