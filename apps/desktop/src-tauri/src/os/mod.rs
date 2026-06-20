use std::{
    fs::{File, OpenOptions},
    path::Path,
    process::Command,
};

pub mod clipboard;
pub mod dialogs;

#[cfg(windows)]
mod windows;

#[cfg(windows)]
mod job_object;
#[cfg(windows)]
pub use job_object::JobObject;

pub fn configure_no_window(command: &mut Command) {
    #[cfg(windows)]
    windows::configure_no_window(command);

    #[cfg(not(windows))]
    let _ = command;
}

pub fn open_folder(path: &Path) -> std::io::Result<()> {
    #[cfg(windows)]
    return windows::open_folder(path);

    #[cfg(target_os = "macos")]
    return Command::new("open").arg(path).spawn().map(|_| ());

    #[cfg(all(unix, not(target_os = "macos")))]
    return Command::new("xdg-open").arg(path).spawn().map(|_| ());
}

pub fn reveal_file(path: &str) -> std::io::Result<()> {
    #[cfg(windows)]
    return windows::reveal_file(path);

    #[cfg(not(windows))]
    return open_folder(Path::new(path));
}

pub fn copy_text_to_clipboard(app: &tauri::AppHandle, text: &str) -> std::io::Result<()> {
    clipboard::copy_text_to_clipboard(app, text)
}

pub fn select_document_files(app: &tauri::AppHandle) -> std::io::Result<Vec<String>> {
    dialogs::select_document_files(app)
}

pub fn select_connected_folder(app: &tauri::AppHandle) -> std::io::Result<Option<String>> {
    dialogs::select_connected_folder(app)
}

pub fn open_append(path: &Path) -> std::io::Result<File> {
    OpenOptions::new().create(true).append(true).open(path)
}
