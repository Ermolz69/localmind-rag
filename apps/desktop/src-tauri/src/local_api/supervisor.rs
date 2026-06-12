use std::{sync::Mutex, thread, time::Duration};

use tauri::{AppHandle, Emitter, Manager};

use crate::{
    app_runtime::{local_api_base_url, SupervisorError, HEALTH_PATH, STATUS_EVENT},
    local_api::{
        health, paths, process,
        state::{AppRuntimeInfo, DesktopMode, LocalApiStatus, SupervisorState},
    },
};

pub struct LocalApiSupervisor {
    state: Mutex<SupervisorState>,
}

impl LocalApiSupervisor {
    pub fn new() -> Self {
        Self {
            state: Mutex::new(SupervisorState::default()),
        }
    }

    pub fn runtime_info(&self) -> AppRuntimeInfo {
        let root = paths::app_root();
        let state = self.state.lock().expect("supervisor state poisoned");

        AppRuntimeInfo {
            local_api_status: state.status,
            base_url: state
                .override_url
                .clone()
                .or_else(|| state.port.map(local_api_base_url)),
            pid: state.child.as_ref().map(std::process::Child::id),
            logs_path: paths::logs_dir(&root).display().to_string(),
            app_data_path: paths::app_data_dir(&root).display().to_string(),
            last_error: state.last_error.clone(),
            desktop_mode: state.desktop_mode,
            api_available: state.status == LocalApiStatus::Ready
                && (state.port.is_some() || state.override_url.is_some()),
        }
    }

    pub fn start(&self, app: AppHandle, restarting: bool) {
        self.start_with_retry(app, restarting, 1);
    }

    fn start_with_retry(&self, app: AppHandle, restarting: bool, attempt: u32) {
        if !self.transition_to_starting(&app, restarting) {
            return;
        }

        let root = paths::app_root();
        if let Err(error) = paths::ensure_runtime_dirs(&root) {
            self.set_failed(&app, SupervisorError::Io(error.to_string()));
            return;
        }

        if let Ok(override_url) = std::env::var("LOCALMIND_LOCAL_API_URL") {
            paths::write_sidecar_log(
                &root,
                &format!("Using external LocalApi override from environment: {override_url}"),
            );

            let (generation, token) = {
                let mut state = self.state.lock().expect("supervisor state poisoned");
                state.status = LocalApiStatus::Ready;
                state.override_url = Some(override_url.clone());
                state.monitor_running = true;
                state.last_error = None;
                state.monitor_generation += 1;
                state.instance_token = None;
                (state.monitor_generation, String::new())
            };

            self.emit_status(&app);
            self.spawn_monitor(app, root, override_url, token, generation, attempt);
            return;
        }

        let port = process::reserve_loopback_port();
        if let Err(error) = paths::write_sidecar_port(&root, port) {
            self.set_failed(&app, SupervisorError::Io(error.to_string()));
            return;
        }

        let token = uuid::Uuid::new_v4().to_string();

        match process::spawn_local_api(&root, port, &token) {
            Ok((child, description)) => {
                paths::write_sidecar_log(
                    &root,
                    &format!(
                        "Starting LocalApi with command: {description} (Attempt {})",
                        attempt
                    ),
                );

                let generation = {
                    let mut state = self.state.lock().expect("supervisor state poisoned");
                    state.child = Some(child);
                    state.port = Some(port);
                    state.status = LocalApiStatus::Starting;
                    state.monitor_running = true;
                    state.last_error = None;
                    state.monitor_generation += 1;
                    state.instance_token = Some(token.clone());
                    state.monitor_generation
                };

                self.emit_status(&app);
                self.spawn_monitor(
                    app,
                    root,
                    local_api_base_url(port),
                    token,
                    generation,
                    attempt,
                );
            }
            Err(error) => {
                paths::write_sidecar_log(&root, &format!("Spawn failed: {}", error.message()));
                self.set_failed(&app, error);
            }
        }
    }

    pub fn restart(&self, app: AppHandle) {
        let child_to_kill = {
            let mut state = self.state.lock().expect("supervisor state poisoned");
            state.status = LocalApiStatus::Restarting;
            state.last_error = None;
            state.monitor_running = false;
            state.port = None;
            state.instance_token = None;
            state.monitor_generation += 1;
            state.child.take()
        };

        if let Some(mut child) = child_to_kill {
            process::kill_child(&mut child);
        }

        self.emit_status(&app);
        self.start_with_retry(app, true, 1);
    }

    pub fn stop(&self, app: &AppHandle) {
        let child_to_kill = {
            let mut state = self.state.lock().expect("supervisor state poisoned");
            state.status = LocalApiStatus::Stopped;
            state.monitor_running = false;
            state.port = None;
            state.instance_token = None;
            state.monitor_generation += 1;
            state.child.take()
        };

        if let Some(mut child) = child_to_kill {
            process::kill_child(&mut child);
        }

        self.emit_status(app);
    }

    pub fn enable_limited_mode(&self, app: &AppHandle) {
        {
            let mut state = self.state.lock().expect("supervisor state poisoned");
            state.desktop_mode = DesktopMode::Limited;
        }
        self.emit_status(app);
    }

    fn transition_to_starting(&self, app: &AppHandle, restarting: bool) -> bool {
        {
            let mut state = self.state.lock().expect("supervisor state poisoned");
            if matches!(
                state.status,
                LocalApiStatus::Starting | LocalApiStatus::Ready
            ) {
                return false;
            }

            state.status = if restarting {
                LocalApiStatus::Restarting
            } else {
                LocalApiStatus::Starting
            };
            state.last_error = None;
        }

        self.emit_status(app);
        true
    }

    fn set_status(
        &self,
        app: &AppHandle,
        status: LocalApiStatus,
        port: Option<u16>,
        last_error: Option<String>,
        monitor_running: bool,
    ) {
        {
            let mut state = self.state.lock().expect("supervisor state poisoned");
            state.status = status;
            state.port = port.or(state.port);
            state.last_error = last_error;
            state.monitor_running = monitor_running;
        }

        self.emit_status(app);
    }

    fn set_failed(&self, app: &AppHandle, error: SupervisorError) {
        let root = paths::app_root();
        paths::write_sidecar_log(&root, &error.message());
        self.set_status(
            app,
            LocalApiStatus::Failed,
            None,
            Some(error.message()),
            false,
        );
    }

    fn spawn_monitor(
        &self,
        app: AppHandle,
        root: std::path::PathBuf,
        base_url: String,
        expected_token: String,
        generation: u64,
        attempt: u32,
    ) {
        thread::spawn(move || {
            let ready = if expected_token.is_empty() {
                true
            } else {
                health::wait_for_health(&base_url, &expected_token)
            };

            let mut retry = false;

            {
                let supervisor = app.state::<LocalApiSupervisor>();
                let mut state = supervisor.state.lock().expect("supervisor state poisoned");

                if !state.monitor_running || state.monitor_generation != generation {
                    return;
                }

                if ready {
                    state.status = LocalApiStatus::Ready;
                    state.last_error = None;
                } else {
                    state.monitor_running = false;
                    if attempt < 3 {
                        retry = true;
                    } else {
                        state.status = LocalApiStatus::Failed;
                        state.last_error = Some(
                            SupervisorError::HealthTimeout(format!("{base_url}{HEALTH_PATH}"))
                                .message(),
                        );
                    }
                }
            }

            if retry {
                paths::write_sidecar_log(
                    &root,
                    &format!(
                        "Startup attempt {}/3 failed health check for {}{}. Identity mismatch or port collision. Retrying with a new port.",
                        attempt, base_url, HEALTH_PATH
                    ),
                );

                let supervisor = app.state::<LocalApiSupervisor>();
                let child_to_kill = {
                    let mut state = supervisor.state.lock().expect("supervisor state poisoned");
                    state.status = LocalApiStatus::Restarting;
                    state.port = None;
                    state.instance_token = None;
                    state.monitor_generation += 1;
                    state.child.take()
                };

                if let Some(mut child) = child_to_kill {
                    process::kill_child(&mut child);
                }

                supervisor.emit_status(&app);
                supervisor.start_with_retry(app.clone(), true, attempt + 1);
                return;
            }

            let _ = app.emit(
                STATUS_EVENT,
                app.state::<LocalApiSupervisor>().runtime_info(),
            );

            loop {
                thread::sleep(Duration::from_secs(1));

                let mut crashed = false;
                {
                    let supervisor = app.state::<LocalApiSupervisor>();
                    let mut state = supervisor.state.lock().expect("supervisor state poisoned");
                    if !state.monitor_running || state.monitor_generation != generation {
                        return;
                    }

                    if let Some(child) = state.child.as_mut() {
                        if let Ok(Some(exit_status)) = child.try_wait() {
                            state.status = LocalApiStatus::Crashed;
                            state.last_error =
                                Some(format!("LocalApi exited with status {exit_status}."));
                            state.child = None;
                            state.monitor_running = false;
                            crashed = true;
                        }
                    }
                }

                if crashed {
                    paths::write_sidecar_log(&root, "LocalApi process crashed.");
                    let _ = app.emit(
                        STATUS_EVENT,
                        app.state::<LocalApiSupervisor>().runtime_info(),
                    );
                    return;
                }
            }
        });
    }

    fn emit_status(&self, app: &AppHandle) {
        let _ = app.emit(STATUS_EVENT, self.runtime_info());
    }
}

impl Drop for LocalApiSupervisor {
    fn drop(&mut self) {
        let child_to_kill = if let Ok(mut state) = self.state.lock() {
            state.child.take()
        } else {
            None
        };

        if let Some(mut child) = child_to_kill {
            process::kill_child(&mut child);
        }
    }
}
