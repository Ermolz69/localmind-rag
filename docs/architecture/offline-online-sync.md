# Offline/Online Sync

LocalMind is offline-first. Local workflows do not require remote services: upload, ingestion, search, notes, settings, runtime diagnostics, and local chat history all operate against local storage.

## Current State

- `sync_state` tracks local sync state.
- `sync_outbox` records local operations that can later be pushed.
- Sync endpoints expose skeleton status/run/push/pull behavior through LocalApi.
- Remote sync is not the source of truth for local reads or writes.

## Ownership Rule

Local SQLite and local files own desktop state. Future remote sync must adapt to that local model instead of requiring the desktop app to become online-first.

See [Data and storage](./database.md) for the local persistence model.
