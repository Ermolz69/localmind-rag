use std::{
    env,
    fs::{self, File, OpenOptions},
    io::Write,
    path::{Path, PathBuf},
    time::{SystemTime, UNIX_EPOCH},
};

pub fn app_root() -> PathBuf {
    repository_root().unwrap_or_else(portable_root)
}

pub fn app_data_dir(root: &Path) -> PathBuf {
    root.join("runtime").join("app")
}

pub fn data_dir(root: &Path) -> PathBuf {
    app_data_dir(root).join("data")
}

pub fn files_dir(root: &Path) -> PathBuf {
    app_data_dir(root).join("files")
}

pub fn indexes_dir(root: &Path) -> PathBuf {
    app_data_dir(root).join("indexes")
}

pub fn logs_dir(root: &Path) -> PathBuf {
    app_data_dir(root).join("logs")
}

pub fn sidecar_port_path(root: &Path) -> PathBuf {
    data_dir(root).join("sidecar-port.txt")
}

pub fn local_api_binary_path(root: &Path) -> PathBuf {
    let file_name = if cfg!(windows) {
        "KnowledgeApp.LocalApi.exe"
    } else {
        "KnowledgeApp.LocalApi"
    };

    root.join("bin").join(file_name)
}

pub fn local_api_project_path(root: &Path) -> PathBuf {
    root.join("backend")
        .join("src")
        .join("KnowledgeApp.LocalApi")
        .join("KnowledgeApp.LocalApi.csproj")
}

pub fn ensure_runtime_dirs(root: &Path) -> std::io::Result<()> {
    fs::create_dir_all(data_dir(root))?;
    fs::create_dir_all(files_dir(root))?;
    fs::create_dir_all(indexes_dir(root))?;
    fs::create_dir_all(logs_dir(root))
}

pub fn read_sidecar_port(root: &Path) -> Option<u16> {
    fs::read_to_string(sidecar_port_path(root))
        .ok()
        .and_then(|content| content.trim().parse::<u16>().ok())
}

pub fn write_sidecar_port(root: &Path, port: u16) -> std::io::Result<()> {
    fs::write(sidecar_port_path(root), port.to_string())
}

pub fn write_sidecar_log(root: &Path, message: &str) {
    if let Some(mut file) = open_sidecar_log(root) {
        let timestamp = SystemTime::now()
            .duration_since(UNIX_EPOCH)
            .map(|duration| duration.as_secs())
            .unwrap_or_default();
        let _ = writeln!(file, "[{timestamp}] {message}");
    }
}

fn open_sidecar_log(root: &Path) -> Option<File> {
    OpenOptions::new()
        .create(true)
        .append(true)
        .open(logs_dir(root).join("localapi-sidecar.log"))
        .ok()
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
