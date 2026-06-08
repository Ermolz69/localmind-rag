use std::{
    fs::{File, OpenOptions},
    path::Path,
    process::Command,
};

#[cfg(windows)]
mod windows;

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

pub fn copy_text_to_clipboard(text: &str) -> std::io::Result<()> {
    #[cfg(windows)]
    return windows::copy_text_to_clipboard(text);

    #[cfg(not(windows))]
    {
        let _ = text;
        Ok(())
    }
}

pub fn select_document_files() -> std::io::Result<Vec<String>> {
    #[cfg(windows)]
    return windows::select_document_files();

    #[cfg(not(windows))]
    Ok([])
}

pub fn select_connected_folder() -> std::io::Result<Option<String>> {
    #[cfg(windows)]
    return windows::select_connected_folder();

    #[cfg(not(windows))]
    Ok(None)
}

pub fn open_append(path: &Path) -> std::io::Result<File> {
    OpenOptions::new().create(true).append(true).open(path)
}
