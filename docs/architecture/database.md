# Data And Storage

LocalMind persists user data locally by default. SQLite stores structured application state, uploaded source files live in managed runtime folders, and embeddings stay local behind vector-search interfaces.

```mermaid
erDiagram
    BUCKETS ||--o{ DOCUMENTS : contains
    BUCKETS ||--o{ NOTES : groups
    DOCUMENTS ||--|| DOCUMENT_FILES : stores_original
    DOCUMENTS ||--o{ DOCUMENT_CHUNKS : splits_into
    DOCUMENT_CHUNKS ||--|| DOCUMENT_EMBEDDINGS : embeds
    DOCUMENTS ||--o{ INGESTION_JOBS : schedules
    CONVERSATIONS ||--o{ CHAT_MESSAGES : has
    NOTES ||--o{ NOTE_LINKS : links
    DOCUMENTS ||--o{ SYNC_OUTBOX : queues
    NOTES ||--o{ SYNC_OUTBOX : queues
```

## SQLite

SQLite is the local database for buckets, documents, notes, chats, ingestion jobs, settings, diagnostics state, sync state, chunks, and embeddings. EF Core migrations own schema changes.

| Table | Purpose |
| --- | --- |
| `buckets` | User-created document/note grouping. |
| `documents`, `document_files` | Document metadata and managed local file references. |
| `ingestion_jobs` | Durable ingestion lifecycle, progress, retry/cancel state, and sanitized failures. |
| `document_chunks`, `document_embeddings`, `document_chunks_fts` | Searchable chunks, local embedding vectors, and SQLite FTS/BM25 keyword index rows. |
| `notes`, `note_links` | Local notes and note relationships. |
| `conversations`, `chat_messages` | RAG chat history. |
| `app_settings` | Local settings such as selected bucket and runtime preferences. |
| `sync_state`, `sync_outbox` | Local sync skeleton state. |

## Files And Indexes

SQLite stores the FTS virtual table in the local database. Portable mode stores runtime files under `runtime/app`:

```text
runtime/app/data      SQLite database
runtime/app/files     uploaded source files
runtime/app/indexes   local vector/search indexes
runtime/app/logs      local logs and diagnostic events
```

Uploads are copied into `runtime/app/files/{documentId}/` with sanitized file names. The app does not expose arbitrary disk import paths through LocalApi.

## Offline Mode

Documents, chunks, embeddings, notes, chats, and settings are available offline. Remote sync is a separate future boundary and does not own local-first behavior.
