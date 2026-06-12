use std::{
    net::TcpListener,
    path::Path,
    process::{Child, Command, Stdio},
};

use crate::{
    app_runtime::{SupervisorError, DEFAULT_LOCAL_API_PORT, LOCAL_API_HOST},
    local_api::paths,
    os,
};

pub struct LocalApiLaunch {
    pub command: Command,
    pub description: String,
}

pub fn reserve_loopback_port() -> u16 {
    TcpListener::bind((LOCAL_API_HOST, 0))
        .ok()
        .and_then(|listener| listener.local_addr().ok())
        .map(|addr| addr.port())
        .unwrap_or(DEFAULT_LOCAL_API_PORT)
}

pub fn spawn_local_api(
    root: &Path,
    port: u16,
    token: &str,
) -> Result<(Child, String), SupervisorError> {
    let Some(mut launch) = build_local_api_command(root) else {
        return Err(SupervisorError::BinaryNotFound);
    };

    let base_url = crate::app_runtime::local_api_base_url(port);
    let log_file = os::open_append(&paths::logs_dir(root).join("localapi-sidecar.log")).ok();

    launch
        .command
        .current_dir(root)
        .env("KNOWLEDGE_APP_ROOT", root)
        .env("ASPNETCORE_URLS", &base_url)
        .env("LOCALMIND_LOCAL_API_PORT", port.to_string())
        .env("LOCALMIND_SUPERVISOR_TOKEN", token)
        .env("LocalRuntime__DataPath", paths::data_dir(root))
        .env("LocalRuntime__FilesPath", paths::files_dir(root))
        .env("LocalRuntime__IndexPath", paths::indexes_dir(root))
        .env("LocalRuntime__LogsPath", paths::logs_dir(root))
        .stdin(Stdio::null())
        .stdout(
            log_file
                .as_ref()
                .and_then(|file| file.try_clone().ok())
                .map_or(Stdio::null(), Stdio::from),
        )
        .stderr(log_file.map_or(Stdio::null(), Stdio::from));

    os::configure_no_window(&mut launch.command);

    launch
        .command
        .spawn()
        .map(|child| (child, launch.description))
        .map_err(|error| SupervisorError::SpawnFailed(error.to_string()))
}

pub fn kill_child(child: &mut Child) {
    let _ = child.kill();
    let _ = child.wait();
}

fn build_local_api_command(root: &Path) -> Option<LocalApiLaunch> {
    let sidecar_path = paths::local_api_binary_path(root);

    if sidecar_path.exists() {
        let mut command = Command::new(&sidecar_path);

        command
            .env("ASPNETCORE_ENVIRONMENT", "Production")
            .env("DOTNET_ENVIRONMENT", "Production");

        return Some(LocalApiLaunch {
            command,
            description: sidecar_path.display().to_string(),
        });
    }

    let project_path = paths::local_api_project_path(root);

    if project_path.exists() {
        let mut command = Command::new("dotnet");

        command
            .arg("run")
            .arg("--no-launch-profile")
            .arg("--project")
            .arg(&project_path)
            .env("ASPNETCORE_ENVIRONMENT", "Development")
            .env("DOTNET_ENVIRONMENT", "Development");

        return Some(LocalApiLaunch {
            command,
            description: format!(
                "dotnet run --no-launch-profile --project {}",
                project_path.display()
            ),
        });
    }

    None
}
