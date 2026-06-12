# Desktop Tauri Supervisor

The Rust/Tauri layer is a desktop and system integration layer. It is not a second backend.

React owns UI. Tauri/Rust owns desktop lifecycle and system integration. `KnowledgeApp.LocalApi` owns RAG, SQLite, ingestion, sync, chat, and AI runtime business logic.

## Responsibilities

Rust owns:

- Starting `KnowledgeApp.LocalApi`.
- LocalApi health and readiness.
- Process supervision, crash detection, restart, and shutdown.
- App runtime paths.
- LocalApi sidecar logs.
- System dialogs.
- Opening folders and revealing files.
- Events sent to the frontend.
- Future secure token storage.

Rust must not own:

- RAG pipeline logic.
- SQLite domain logic.
- Embeddings or model adaptation.
- Document indexing.
- Sync business logic.
- Chat logic.

## Source Layout

Tauri source is organized by side-effect boundary:

```text
apps/desktop/src-tauri/src/
├─ main.rs
├─ app_runtime.rs
├─ local_api/
│  ├─ mod.rs
│  ├─ commands.rs
│  ├─ health.rs
│  ├─ paths.rs
│  ├─ process.rs
│  ├─ state.rs
│  └─ supervisor.rs
└─ os/
   ├─ mod.rs
   └─ windows.rs
```

`main.rs` is only the Tauri composition root. It declares modules, registers managed state, registers commands, starts LocalApi on setup, handles shutdown, and runs the app.

`local_api/state.rs` contains status and frontend-facing DTOs.

`local_api/supervisor.rs` owns status transitions and event emission.

`local_api/process.rs` starts and stops the LocalApi process.

`local_api/health.rs` owns health polling and readiness.

`local_api/paths.rs` owns runtime path construction and sidecar log/port files.

`local_api/commands.rs` contains thin Tauri command wrappers.

`os/` contains OS-specific integration such as Windows no-window process flags, Explorer integration, clipboard, and native dialogs.

## Status Machine

Frontend receives `local-api-status-changed` events with `AppRuntimeInfo`.

Statuses:

- `NotStarted`
- `Starting`
- `Ready`
- `Failed`
- `Crashed`
- `Restarting`
- `Stopped`

`baseUrl` is returned only when LocalApi is `Ready`. The frontend must not guess a port.

## Startup Flow

1. Tauri creates `LocalApiSupervisor`.
2. Tauri setup calls `start_local_api_on_setup`.
3. Supervisor sets status to `Starting` and emits an event.
4. Supervisor reuses an already healthy LocalApi from `sidecar-port.txt` when available.
5. Otherwise it reserves a loopback port, writes `sidecar-port.txt`, starts LocalApi, and passes `ASPNETCORE_URLS=http://127.0.0.1:<port>`.
6. Readiness polling calls only `GET /api/v1/health`.
7. On success, status becomes `Ready`.
8. On timeout, status becomes `Failed`.
9. If the process exits after startup, status becomes `Crashed`.
10. On window close, supervisor stops LocalApi.

Backoff schedule:

```text
0ms, 250ms, 500ms, 1s, 2s, 3s, 4s, then 5s for 30 attempts
```

## Frontend Contract

Commands:

- `get_app_runtime_info()`
- `restart_local_api()`
- `open_logs_folder()`
- `copy_diagnostics_to_clipboard()`
- `select_document_files()`
- `select_connected_folder()`
- `reveal_file_in_explorer(path)`

Events:

- `local-api-status-changed`

Command payloads and event payloads must be stable `serde` DTOs. Internal Rust state is not exposed directly.

## Rust Coding Standard

- `main.rs` is only the Tauri composition root.
- LocalApi supervision lives under `local_api/`.
- OS-specific helpers live under `os/`.
- No RAG, indexing, SQLite, sync, chat, or AI runtime business logic in Rust.
- No fixed LocalApi URL when dynamic port is enabled.
- `Port` is stored in supervisor state.
- `baseUrl` is built from the current port.
- No duplicated imports.
- No unused imports.
- No wildcard imports outside tests or prelude-style modules.
- No `unsafe`.
- No long-running work while holding a `Mutex`.
- Only supervisor methods mutate `LocalApiStatus`.
- Each status change emits `local-api-status-changed`.
- All frontend-facing payloads are `serde` DTOs.
- All event names and API paths are constants.
- Tauri commands are thin wrappers over supervisor, path, process, health, or OS helpers.
- New crates require a clear reason.

## Error Rules

Internal Rust errors use typed enums such as `SupervisorError`. Tauri command boundaries return serializable `ErrorDto` values.

Important errors should not be returned as bare strings unless the call site is intentionally best-effort and not part of the frontend contract.

## Mutex Rules

The supervisor mutex is held only while reading or writing state.

Do not hold a mutex while:

- spawning a process;
- sleeping;
- polling health;
- doing file IO;
- doing network IO;
- calling OS commands.

## Required Checks

Rust code is ready only after these checks pass:

```bash
cargo fmt --manifest-path apps/desktop/src-tauri/Cargo.toml --check
cargo clippy --manifest-path apps/desktop/src-tauri/Cargo.toml -- -D warnings
cargo check --manifest-path apps/desktop/src-tauri/Cargo.toml
```

On Windows, `cargo check` and `cargo clippy` require Visual Studio Build Tools with the C++ workload because the MSVC linker `link.exe` is required.
