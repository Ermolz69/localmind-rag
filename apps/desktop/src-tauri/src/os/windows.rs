use std::{path::Path, process::Command};

use std::os::windows::process::CommandExt;

const CREATE_NO_WINDOW: u32 = 0x0800_0000;

pub fn configure_no_window(command: &mut Command) {
    command.creation_flags(CREATE_NO_WINDOW);
}

pub fn open_folder(path: &Path) -> std::io::Result<()> {
    Command::new("explorer").arg(path).spawn().map(|_| ())
}

pub fn reveal_file(path: &str) -> std::io::Result<()> {
    Command::new("explorer")
        .arg(format!("/select,{path}"))
        .spawn()
        .map(|_| ())
}
