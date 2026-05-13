#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

use std::{
    env,
    fs::{self, File, OpenOptions},
    io::Write,
    net::{TcpStream, ToSocketAddrs},
    path::{Path, PathBuf},
    process::{Child, Command, Stdio},
    sync::Mutex,
    time::Duration,
};

use tauri::Manager;

#[cfg(windows)]
use std::os::windows::process::CommandExt;

#[cfg(windows)]
const CREATE_NO_WINDOW: u32 = 0x0800_0000;

const LOCAL_API_URL: &str = "http://127.0.0.1:49321";
const LOCAL_API_SOCKET: &str = "127.0.0.1:49321";

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

    if is_local_api_running() {
        write_sidecar_log(
            &app_root,
            "LocalApi is already listening on 127.0.0.1:49321.",
        );
        return;
    }

    let Some(launch) = local_api_launch() else {
        write_sidecar_log(&app_root, "LocalApi sidecar was not found.");
        return;
    };

    let LocalApiLaunch {
        root,
        mut command,
        description,
    } = launch;

    let _ = fs::create_dir_all(root.join("runtime").join("app").join("logs"));
    write_sidecar_log(
        &root,
        &format!("Starting LocalApi with command: {description}"),
    );

    let log_file = open_sidecar_log(&root);

    command
        .current_dir(&root)
        .env("KNOWLEDGE_APP_ROOT", &root)
        .env("ASPNETCORE_URLS", LOCAL_API_URL)
        .stdin(Stdio::null())
        .stdout(
            log_file
                .as_ref()
                .and_then(|file| file.try_clone().ok())
                .map_or(Stdio::null(), Stdio::from),
        )
        .stderr(log_file.map_or(Stdio::null(), Stdio::from));

    #[cfg(windows)]
    command.creation_flags(CREATE_NO_WINDOW);

    match command.spawn() {
        Ok(child) => {
            let state = app.state::<LocalApiProcess>();
            if let Ok(mut stored_child) = state.child.lock() {
                *stored_child = Some(child);
            };
        }
        Err(error) => {
            write_sidecar_log(&root, &format!("Failed to start LocalApi sidecar: {error}"))
        }
    }
}

struct LocalApiLaunch {
    root: PathBuf,
    command: Command,
    description: String,
}

fn local_api_launch() -> Option<LocalApiLaunch> {
    let app_root = portable_root();
    let sidecar_path = local_api_path(&app_root);
    if sidecar_path.exists() {
        let mut command = Command::new(&sidecar_path);
        command
            .env("ASPNETCORE_ENVIRONMENT", "Production")
            .env("DOTNET_ENVIRONMENT", "Production");

        return Some(LocalApiLaunch {
            root: app_root,
            command,
            description: sidecar_path.display().to_string(),
        });
    }

    let repo_root = repository_root().unwrap_or(app_root);
    let project_path = repo_root
        .join("backend")
        .join("src")
        .join("KnowledgeApp.LocalApi")
        .join("KnowledgeApp.LocalApi.csproj");

    if project_path.exists() {
        let mut command = Command::new("dotnet");
        command
            .arg("run")
            .arg("--project")
            .arg(&project_path)
            .arg("--urls")
            .arg(LOCAL_API_URL)
            .env("ASPNETCORE_ENVIRONMENT", "Development")
            .env("DOTNET_ENVIRONMENT", "Development");

        return Some(LocalApiLaunch {
            root: repo_root,
            command,
            description: format!("dotnet run --project {}", project_path.display()),
        });
    }

    None
}

fn portable_root() -> PathBuf {
    env::current_exe()
        .ok()
        .and_then(|path| path.parent().map(Path::to_path_buf))
        .unwrap_or_else(|| env::current_dir().unwrap_or_else(|_| PathBuf::from(".")))
}

fn repository_root() -> Option<PathBuf> {
    let mut current = env::current_dir().ok();
    while let Some(path) = current {
        if path.join("pnpm-workspace.yaml").exists() {
            return Some(path);
        }

        current = path.parent().map(Path::to_path_buf);
    }

    None
}

fn local_api_path(app_root: &Path) -> PathBuf {
    let file_name = if cfg!(windows) {
        "KnowledgeApp.LocalApi.exe"
    } else {
        "KnowledgeApp.LocalApi"
    };

    app_root.join("bin").join(file_name)
}

fn is_local_api_running() -> bool {
    let Ok(mut addrs) = LOCAL_API_SOCKET.to_socket_addrs() else {
        return false;
    };

    let Some(addr) = addrs.next() else {
        return false;
    };

    TcpStream::connect_timeout(&addr, Duration::from_millis(200)).is_ok()
}

fn open_sidecar_log(root: &Path) -> Option<File> {
    let path = root
        .join("runtime")
        .join("app")
        .join("logs")
        .join("localapi-sidecar.log");

    OpenOptions::new().create(true).append(true).open(path).ok()
}

fn write_sidecar_log(root: &Path, message: &str) {
    if let Some(mut file) = open_sidecar_log(root) {
        let _ = writeln!(file, "{message}");
    }
}
