# Watched Folders

LocalMind can monitor local filesystem folders and automatically queue new or changed files for ingestion, so users do not need to manually upload every document.

## Settings Shape

Watched-folder configuration lives in `app_settings` under the `WatchedFolders` key and is exposed through the standard settings endpoints. The configurable fields are:

- **Enabled** — global toggle for watched-folder auto-ingestion. When off, no watchers run and no files are queued automatically.
- **Folders** — the configured folders. Each entry has an absolute **Path**, an **Enabled** flag, and **IncludeSubdirectories** for watching nested folders.
- **DebounceMilliseconds** — delay after a filesystem event before treating it as stable (avoids ingesting files mid-write). Validated to the 250–60000 ms range.
- **DeletePolicy** — what to do when a watched file is deleted. The only supported value is `MarkDeleted`: the file's `WatchedFileLink` is flagged as deleted and its document waits for an explicit cleanup (see [Delete Policy](#delete-policy)).
- **IgnoredFolders** — folder names skipped during enumeration (defaults: `.git`, `node_modules`, `bin`, `obj`).
- **IgnoredPatterns** — filename globs to skip (defaults: `~$*`, `*.tmp`, `*.bak`).
- **MaxFileSizeMb** — files larger than this are skipped (default 100).
- **AllowedExtensions** — restricts ingestion to these extensions; `null` means all supported document types.
- **StorageMode** — `LinkOnly` (default) keeps the original file in place and records a `WatchedFileLink`; `CopyToAppStorage` copies the file into `runtime/app/files/{documentId}/` like a normal upload.

## Watcher Lifecycle

On startup, the infrastructure layer creates a `FileSystemWatcher` for each configured folder. The watcher raises events for file creation, modification, and deletion. Each event is debounced: a timer resets on each event for the same path, and only the final stable event triggers an ingestion decision.

When watched-folder ingestion is enabled and a new or changed file passes the ignore, size, and extension filters, the backend creates a document record and queues a `Pending` ingestion job exactly as if the file had been uploaded through LocalApi. Unchanged files (matching content hash) are skipped.

On shutdown, the watcher is stopped before the host exits. A guard prevents race conditions where an in-flight debounce timer fires after the watcher has begun stopping.

## Endpoints

- `GET /api/v1/watched-folders/status` — returns per-folder watcher state (health status, pending debounce events, active document count, files awaiting cleanup, last-scan counts) plus sanitized error messages for folders that could not be watched (e.g. path does not exist, access denied).
- `POST /api/v1/watched-folders/rescan` — manually rescans for files that may have been missed (e.g. after a buffer overflow or while the watcher was stopped). Body: `{ "path": "..." }` to rescan one folder, or `{ "path": null }` to rescan all configured folders.
- `POST /api/v1/watched-folders/cleanup` — purges files previously marked deleted (see [Delete Policy](#delete-policy)), removing their `WatchedFileLink`, document, chunks, embeddings, ingestion jobs, and tags. Copied files under `runtime/app/files` are deleted; `LinkOnly` originals in user folders are never touched.

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

Deletion is a two-phase, non-destructive process so that an accidental or temporary file removal never silently purges indexed data. The only supported `DeletePolicy` is `MarkDeleted`.

**Phase 1 — mark.** When a watched file disappears (a watcher `Deleted` event, or a rescan that no longer finds it), its `WatchedFileLink` is stamped with `DeletedAt` and the associated `Document` is soft-deleted (status `Deleted`, sync status `DeletedLocal`). The chunks, embeddings, and search rows are left intact. Folder status reports these as files awaiting cleanup, and the document reflects that its source file is no longer present.

**Phase 2 — cleanup.** `POST /api/v1/watched-folders/cleanup` (also surfaced as "Cleanup deleted files" in Settings) purges every marked link in a single transaction: it removes the `WatchedFileLink`, the `Document`, its chunks, embeddings, ingestion jobs, document-file records, and tags. For `CopyToAppStorage` entries the copied file under `runtime/app/files/{documentId}/` is deleted; `LinkOnly` originals are never deleted — the file storage service only removes paths inside `runtime/app/files`, and the source file is already gone.

If a previously deleted file reappears (re-created or renamed back), the watcher clears `DeletedAt`, restores the document, and re-queues ingestion instead of waiting for cleanup.
