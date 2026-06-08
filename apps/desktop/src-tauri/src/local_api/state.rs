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
}

pub(super) struct SupervisorState {
    pub child: Option<Child>,
    pub status: LocalApiStatus,
    pub port: Option<u16>,
    pub last_error: Option<String>,
    pub monitor_running: bool,
}

impl Default for SupervisorState {
    fn default() -> Self {
        Self {
            child: None,
            status: LocalApiStatus::NotStarted,
            port: None,
            last_error: None,
            monitor_running: false,
        }
    }
}
