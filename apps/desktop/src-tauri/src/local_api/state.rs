use serde::Serialize;
use std::process::Child;

#[derive(Clone, Copy, Debug, Eq, PartialEq, Serialize)]
pub enum LocalApiStatus {
    NotStarted,
    Starting,
    Ready,
    Failed,
    Crashed,
    Restarting,
    Stopped,
}

#[derive(Clone, Copy, Debug, Eq, PartialEq, Serialize)]
pub enum DesktopMode {
    Normal,
    Limited,
}

#[derive(Clone, Debug, Serialize)]
pub struct AppRuntimeInfo {
    #[serde(rename = "localApiStatus")]
    pub local_api_status: LocalApiStatus,
    #[serde(rename = "baseUrl")]
    pub base_url: Option<String>,
    pub pid: Option<u32>,
    #[serde(rename = "logsPath")]
    pub logs_path: String,
    #[serde(rename = "appDataPath")]
    pub app_data_path: String,
    #[serde(rename = "lastError")]
    pub last_error: Option<String>,
    #[serde(rename = "desktopMode")]
    pub desktop_mode: DesktopMode,
    #[serde(rename = "apiAvailable")]
    pub api_available: bool,
}

pub(super) struct SupervisorState {
    pub child: Option<Child>,
    pub status: LocalApiStatus,
    pub port: Option<u16>,
    pub override_url: Option<String>,
    pub last_error: Option<String>,
    pub monitor_running: bool,
    pub desktop_mode: DesktopMode,
    pub instance_token: Option<String>,
    pub monitor_generation: u64,
}

impl Default for SupervisorState {
    fn default() -> Self {
        Self {
            child: None,
            status: LocalApiStatus::NotStarted,
            port: None,
            override_url: None,
            last_error: None,
            monitor_running: false,
            desktop_mode: DesktopMode::Normal,
            instance_token: None,
            monitor_generation: 0,
        }
    }
}
