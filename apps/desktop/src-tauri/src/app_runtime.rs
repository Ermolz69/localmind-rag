use serde::Serialize;

pub const LOCAL_API_HOST: &str = "127.0.0.1";
pub const DEFAULT_LOCAL_API_PORT: u16 = 49321;
pub const STATUS_EVENT: &str = "local-api-status-changed";
pub const HEALTH_PATH: &str = "/api/v1/health";

pub fn local_api_base_url(port: u16) -> String {
    format!("http://{LOCAL_API_HOST}:{port}")
}

#[derive(Debug)]
pub enum SupervisorError {
    BinaryNotFound,
    SpawnFailed(String),
    HealthTimeout(String),
    Io(String),
}

impl SupervisorError {
    pub fn to_dto(&self) -> ErrorDto {
        match self {
            Self::BinaryNotFound => ErrorDto::new(
                "LOCAL_API_BINARY_NOT_FOUND",
                "LocalApi sidecar was not found.",
                None,
            ),
            Self::SpawnFailed(details) => ErrorDto::new(
                "LOCAL_API_SPAWN_FAILED",
                "Failed to start LocalApi sidecar.",
                Some(details.clone()),
            ),
            Self::HealthTimeout(details) => ErrorDto::new(
                "LOCAL_API_HEALTH_TIMEOUT",
                "LocalApi did not become ready.",
                Some(details.clone()),
            ),
            Self::Io(details) => ErrorDto::new(
                "DESKTOP_IO_FAILED",
                "Desktop system operation failed.",
                Some(details.clone()),
            ),
        }
    }

    pub fn message(&self) -> String {
        let dto = self.to_dto();
        match dto.details {
            Some(details) => format!("{} {}", dto.message, details),
            None => dto.message,
        }
    }
}

#[derive(Debug, Serialize)]
pub struct ErrorDto {
    pub code: String,
    pub message: String,
    pub details: Option<String>,
}

impl ErrorDto {
    pub fn new(code: &str, message: &str, details: Option<String>) -> Self {
        Self {
            code: code.to_string(),
            message: message.to_string(),
            details,
        }
    }
}

impl From<SupervisorError> for ErrorDto {
    fn from(error: SupervisorError) -> Self {
        error.to_dto()
    }
}

impl From<std::io::Error> for ErrorDto {
    fn from(error: std::io::Error) -> Self {
        SupervisorError::Io(error.to_string()).to_dto()
    }
}
