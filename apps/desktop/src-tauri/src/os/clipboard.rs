use tauri::AppHandle;
use tauri_plugin_clipboard_manager::ClipboardExt;

pub fn copy_text_to_clipboard(app: &AppHandle, text: &str) -> std::io::Result<()> {
    app.clipboard().write_text(text.to_string()).map_err(|e| std::io::Error::new(std::io::ErrorKind::Other, e.to_string()))?;
    Ok(())
}
