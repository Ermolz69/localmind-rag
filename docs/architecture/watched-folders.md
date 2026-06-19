# Watched Folders

LocalMind can monitor local filesystem folders and automatically queue new or changed files for ingestion, so users do not need to manually upload every document.

## Settings Shape

Watched-folder configuration lives in `app_settings` under the `WatchedFolders` key and is exposed through the standard settings endpoints. The configurable fields are:

- **Folders** — list of absolute paths to watch.
- **AutoIngestion** — whether newly detected files are queued for ingestion automatically.
- **Debounce** — milliseconds to wait after a filesystem event before treating it as stable (avoids ingesting files mid-write).
- **DeletePolicy** — what to do when a watched file is deleted: `RemoveDocument` removes the document and chunks, `KeepDocument` leaves the indexed data intact.
- **StorageMode** — `LinkOnly` keeps the original file in place and records a `WatchedFileLink`; `CopyToAppStorage` copies the file into `runtime/app/files/{documentId}/` like a normal upload.

## Watcher Lifecycle

On startup, the infrastructure layer creates a `FileSystemWatcher` for each configured folder. The watcher raises events for file creation, modification, and deletion. Each event is debounced: a timer resets on each event for the same path, and only the final stable event triggers an ingestion decision.

When `AutoIngestion` is enabled and a new or changed file matches a supported extension, the backend creates a document record and queues a `Pending` ingestion job exactly as if the file had been uploaded through LocalApi.

On shutdown, the watcher is stopped before the host exits. A guard prevents race conditions where an in-flight debounce timer fires after the watcher has begun stopping.

## Endpoints

- `GET /api/v1/watched-folders/status` — returns current watcher state per configured folder, count of pending debounce events, and sanitized error messages for folders that could not be watched (e.g. path does not exist, access denied).
- `POST /api/v1/watched-folders/rescan` — manually rescans a given folder for files that may have been missed (e.g. after a buffer overflow or while the watcher was stopped). Body: `{ "path": "..." }`.
- `POST /api/v1/watched-folders/cleanup` — removes `WatchedFileLink` records and associated documents for files that no longer exist on disk. Does not delete files from `runtime/app/files` for `LinkOnly` entries since the source file is already gone.

## Storage Modes

| Mode | File location | `WatchedFileLink` record |
| --- | --- | --- |
| `LinkOnly` | Original path on disk | Yes — points to original path |
| `CopyToAppStorage` | `runtime/app/files/{documentId}/` | Yes — points to copied path |

`LinkOnly` is suitable for large files or network shares where copying is impractical. `CopyToAppStorage` gives the same offline guarantees as a manual upload.

## FileSystemWatcher Limitations

`FileSystemWatcher` uses the OS-level notification buffer. On Windows, rapid bulk changes (large folder copies, archive extraction) can overflow the buffer and drop events silently. The watcher logs a warning when buffer overflow is detected. Use `POST /api/v1/watched-folders/rescan` to recover missed files after bulk operations.

Folders on network drives, USB devices, and removable media may deliver delayed or no events depending on the filesystem driver. These paths should be rescanned manually or excluded from auto-ingestion.

## Delete Policy

When a watched file is deleted and `DeletePolicy = RemoveDocument`:

1. The `WatchedFileLink` record is removed.
2. The associated `Document` is soft-deleted and its chunks, embeddings, and FTS rows are removed.
3. If the storage mode was `CopyToAppStorage`, the copied file is deleted from `runtime/app/files/{documentId}/`.

When `DeletePolicy = KeepDocument`, the `WatchedFileLink` is removed but the document and all indexed data remain. The document status reflects that the source file is no longer present.
