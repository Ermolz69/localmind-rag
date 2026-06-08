use std::{
    io::Write,
    path::Path,
    process::{Command, Stdio},
};

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

pub fn copy_text_to_clipboard(text: &str) -> std::io::Result<()> {
    let mut child = Command::new("powershell")
        .arg("-NoProfile")
        .arg("-Command")
        .arg("Set-Clipboard -Value ([Console]::In.ReadToEnd())")
        .stdin(Stdio::piped())
        .spawn()?;

    if let Some(stdin) = child.stdin.as_mut() {
        stdin.write_all(text.as_bytes())?;
    }

    child.wait().map(|_| ())
}

pub fn select_document_files() -> std::io::Result<Vec<String>> {
    select_paths_with_powershell(true)
}

pub fn select_connected_folder() -> std::io::Result<Option<String>> {
    Ok(select_paths_with_powershell(false)?.into_iter().next())
}

fn select_paths_with_powershell(files: bool) -> std::io::Result<Vec<String>> {
    let script = if files {
        r#"Add-Type -AssemblyName System.Windows.Forms; $d = New-Object System.Windows.Forms.OpenFileDialog; $d.Multiselect = $true; if ($d.ShowDialog() -eq 'OK') { $d.FileNames -join "`n" }"#
    } else {
        r#"Add-Type -AssemblyName System.Windows.Forms; $d = New-Object System.Windows.Forms.FolderBrowserDialog; if ($d.ShowDialog() -eq 'OK') { $d.SelectedPath }"#
    };

    let output = Command::new("powershell")
        .arg("-NoProfile")
        .arg("-STA")
        .arg("-Command")
        .arg(script)
        .output()?;

    Ok(String::from_utf8_lossy(&output.stdout)
        .lines()
        .map(str::trim)
        .filter(|line| !line.is_empty())
        .map(ToOwned::to_owned)
        .collect())
}
