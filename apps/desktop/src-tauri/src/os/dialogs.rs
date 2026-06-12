use tauri::AppHandle;
use tauri_plugin_dialog::DialogExt;

pub fn select_document_files(app: &AppHandle) -> std::io::Result<Vec<String>> {
    let result = app.dialog().file().blocking_pick_files();
    let mut files = Vec::new();

    if let Some(paths) = result {
        for path in paths {
            if let Ok(p) = path.into_path() {
                if let Some(s) = p.to_str() {
                    files.push(s.to_string());
                }
            }
        }
    }

    Ok(files)
}

pub fn select_connected_folder(app: &AppHandle) -> std::io::Result<Option<String>> {
    let result = app.dialog().file().blocking_pick_folder();

    if let Some(path) = result {
        if let Ok(p) = path.into_path() {
            if let Some(s) = p.to_str() {
                return Ok(Some(s.to_string()));
            }
        }
    }

    Ok(None)
}
