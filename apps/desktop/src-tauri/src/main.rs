#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

use std::{
    env,
    path::{Path, PathBuf},
    process::{Child, Command, Stdio},
    sync::Mutex,
};

use tauri::Manager;

#[cfg(windows)]
use std::os::windows::process::CommandExt;

#[cfg(windows)]
const CREATE_NO_WINDOW: u32 = 0x0800_0000;

#[derive(Default)]
struct LocalApiProcess {
    child: Mutex<Option<Child>>,
}

impl Drop for LocalApiProcess {
    fn drop(&mut self) {
        if let Ok(mut child) = self.child.lock() {
            if let Some(mut child) = child.take() {
                let _ = child.kill();
                let _ = child.wait();
            }
        }
    }
}

fn main() {
    tauri::Builder::default()
        .manage(LocalApiProcess::default())
        .setup(|app| {
            start_local_api(app);
            Ok(())
        })
        .run(tauri::generate_context!())
        .expect("error while running localmind");
}

fn start_local_api(app: &mut tauri::App) {
    let app_root = portable_root();
    let sidecar_path = local_api_path(&app_root);
    if !sidecar_path.exists() {
        return;
    }

    let mut command = Command::new(sidecar_path);
    command
        .current_dir(&app_root)
        .env("KNOWLEDGE_APP_ROOT", &app_root)
        .env("ASPNETCORE_ENVIRONMENT", "Production")
        .env("DOTNET_ENVIRONMENT", "Production")
        .env("ASPNETCORE_URLS", "http://127.0.0.1:49321")
        .stdin(Stdio::null())
        .stdout(Stdio::null())
        .stderr(Stdio::null());

    #[cfg(windows)]
    command.creation_flags(CREATE_NO_WINDOW);

    if let Ok(child) = command.spawn() {
        let state = app.state::<LocalApiProcess>();
        if let Ok(mut stored_child) = state.child.lock() {
            *stored_child = Some(child);
        }
    }
}

fn portable_root() -> PathBuf {
    env::current_exe()
        .ok()
        .and_then(|path| path.parent().map(Path::to_path_buf))
        .unwrap_or_else(|| env::current_dir().unwrap_or_else(|_| PathBuf::from(".")))
}

fn local_api_path(app_root: &Path) -> PathBuf {
    let file_name = if cfg!(windows) {
        "KnowledgeApp.LocalApi.exe"
    } else {
        "KnowledgeApp.LocalApi"
    };

    app_root.join("bin").join(file_name)
}
