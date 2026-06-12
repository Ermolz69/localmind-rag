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
) -> Result<(), ErrorDto> {
    supervisor.restart(app);
    Ok(())
}

#[tauri::command]
pub fn enable_limited_mode(
    app: AppHandle,
    supervisor: State<'_, LocalApiSupervisor>,
) -> AppRuntimeInfo {
    supervisor.enable_limited_mode(&app);
    supervisor.runtime_info()
}

#[tauri::command]
pub fn read_app_cache(key: String) -> Result<Option<String>, ErrorDto> {
    let root = paths::app_root();
    let path = paths::cache_dir(&root).join(format!("{}.json", key));
    match std::fs::read_to_string(path) {
        Ok(content) => Ok(Some(content)),
        Err(e) if e.kind() == std::io::ErrorKind::NotFound => Ok(None),
        Err(e) => Err(ErrorDto::from(e)),
    }
}

#[tauri::command]
pub fn write_app_cache(key: String, payload: String) -> Result<(), ErrorDto> {
    let root = paths::app_root();
    paths::ensure_runtime_dirs(&root).map_err(ErrorDto::from)?;
    let path = paths::cache_dir(&root).join(format!("{}.json", key));
    std::fs::write(path, payload).map_err(ErrorDto::from)
}

#[tauri::command]
pub fn open_logs_folder() -> Result<(), ErrorDto> {
    let root = paths::app_root();
    paths::ensure_runtime_dirs(&root).map_err(ErrorDto::from)?;
    os::open_folder(&paths::logs_dir(&root)).map_err(ErrorDto::from)
}

#[tauri::command]
pub fn copy_diagnostics_to_clipboard(
    app: AppHandle,
    supervisor: State<'_, LocalApiSupervisor>,
) -> Result<(), ErrorDto> {
    let info = supervisor.runtime_info();
    let version = app.package_info().version.to_string();
    let root = paths::app_root();

    let os_name = std::env::consts::OS;
    let arch = std::env::consts::ARCH;
    let cwd = std::env::current_dir()
        .unwrap_or_default()
        .display()
        .to_string();
    let exe = std::env::current_exe()
        .unwrap_or_default()
        .display()
        .to_string();

    let binary_exists = paths::local_api_binary_path(&root).exists();
    let project_exists = paths::local_api_project_path(&root).exists();
    let runtime_root_mode = paths::runtime_root_mode();

    let timestamp = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap_or_default()
        .as_secs();

    let mut log_tail = String::new();
    if let Ok(content) =
        std::fs::read_to_string(paths::logs_dir(&root).join("localapi-sidecar.log"))
    {
        let lines: Vec<&str> = content.lines().collect();
        let tail = if lines.len() > 20 {
            &lines[lines.len() - 20..]
        } else {
            &lines[..]
        };
        log_tail = tail.join("\n");
    }

    let text = format!(
        "LocalMind Diagnostics\n\
        =====================\n\
        Timestamp: {}\n\
        App Version: {}\n\
        OS: {} ({})\n\
        CWD: {}\n\
        Executable: {}\n\
        Runtime Root: {}\n\
        Runtime Root Mode: {:?}\n\
        Sidecar Binary Path: {}\n\
        Sidecar Project Path: {}\n\
        Sidecar Binary Exists: {}\n\
        Sidecar Project Exists: {}\n\
        \n\
        Supervisor Status: {:?}\n\
        LocalApi PID: {}\n\
        LocalApi Base URL: {}\n\
        Logs Path: {}\n\
        AppData Path: {}\n\
        Last Supervisor Error: {}\n\
        \n\
        --- Last 20 lines of localapi-sidecar.log ---\n\
        {}",
        timestamp,
        version,
        os_name,
        arch,
        cwd,
        exe,
        root.display(),
        runtime_root_mode,
        paths::local_api_binary_path(&root).display(),
        paths::local_api_project_path(&root).display(),
        binary_exists,
        project_exists,
        info.local_api_status,
        info.pid
            .map(|p| p.to_string())
            .unwrap_or_else(|| "None".to_string()),
        info.base_url.unwrap_or_else(|| "None".to_string()),
        info.logs_path,
        info.app_data_path,
        info.last_error.unwrap_or_else(|| "None".to_string()),
        log_tail
    );

    os::copy_text_to_clipboard(&app, &text).map_err(ErrorDto::from)
}

#[tauri::command]
pub fn select_document_files(app: AppHandle) -> Result<Vec<String>, ErrorDto> {
    os::select_document_files(&app).map_err(ErrorDto::from)
}

#[tauri::command]
pub fn select_connected_folder(app: AppHandle) -> Result<Option<String>, ErrorDto> {
    os::select_connected_folder(&app).map_err(ErrorDto::from)
}

#[tauri::command]
pub fn reveal_file_in_explorer(path: String) -> Result<(), ErrorDto> {
    os::reveal_file(&path).map_err(ErrorDto::from)
}
