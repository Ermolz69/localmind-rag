# MVP Architecture And Walking Skeleton

This document describes the current `localmind` MVP architecture, the local database model, and the implemented walking skeleton that proves end-to-end data flow through the desktop UI, local API, application layer, infrastructure, and SQLite database.

## Scope

The current MVP is focused on the local offline-first desktop experience:

- Tauri desktop shell with React UI.
- Local ASP.NET Core API sidecar.
- SQLite local database.
- Local file storage.
- Document upload and ingestion pipeline.
- Notes workspace.
- Diagnostics and runtime status.

The remote sync API exists as a project skeleton, but full remote architecture is out of scope for this MVP documentation because cloud sync is not fully implemented yet.

## Basic Architecture

```mermaid
flowchart TB
    User["User"] --> Desktop["localmind desktop app"]

    subgraph DesktopRuntime["Local desktop runtime"]
        Desktop --> Tauri["Tauri shell"]
        Tauri --> React["React UI"]
        React --> ApiClient["Typed frontend API slices"]
        ApiClient --> LocalApi["KnowledgeApp.LocalApi sidecar"]

        LocalApi --> Endpoints["Minimal API endpoint modules"]
        Endpoints --> AppHandlers["Application handlers and services"]
        AppHandlers --> Ports["Application abstractions"]
        Ports --> Infrastructure["Infrastructure implementations"]

        Infrastructure --> SQLite[("SQLite local DB")]
        Infrastructure --> FileStorage["Local file storage"]
        Infrastructure --> VectorSearch["Local vector search"]
        Infrastructure --> AiRuntime["Local AI runtime adapter"]
    end

    subgraph OptionalFutureCloud["Optional future cloud sync"]
        SyncApi["KnowledgeApp.SyncApi"]
        Postgres[("PostgreSQL")]
        RemoteFiles["Remote file storage"]
    end

    LocalApi -. "future authenticated sync" .-> SyncApi
    SyncApi --> Postgres
    SyncApi --> RemoteFiles
```

## Backend Layering

The backend follows Clean Architecture boundaries:

```mermaid
flowchart TB
    LocalApi["KnowledgeApp.LocalApi"] --> Application["KnowledgeApp.Application"]
    LocalApi --> Contracts["KnowledgeApp.Contracts"]
    LocalApi --> Infrastructure["KnowledgeApp.Infrastructure"]
    LocalApi --> Bootstrap["KnowledgeApp.Bootstrap"]

    Application --> Domain["KnowledgeApp.Domain"]
    Application --> Contracts

    Infrastructure --> Application
    Infrastructure --> Domain
    Infrastructure --> Contracts

    Bootstrap --> Application
    Bootstrap --> Infrastructure

    Domain -. "no EF Core, HTTP, SQLite, Tauri, AI runtime" .-> Domain
    Application -. "depends on abstractions, not Infrastructure" .-> Application
```

Current code locations:

- Desktop UI: `apps/desktop/src`
- Tauri shell: `apps/desktop/src-tauri`
- Local API: `backend/src/KnowledgeApp.LocalApi`
- Application layer: `backend/src/KnowledgeApp.Application`
- Infrastructure: `backend/src/KnowledgeApp.Infrastructure`
- Domain entities: `backend/src/KnowledgeApp.Domain`
- Contracts: `backend/src/KnowledgeApp.Contracts`

## Local Database ER Diagram

SQLite is the main MVP database. It stores local documents, files, chunks, notes, chat history, ingestion jobs, sync outbox state, diagnostics-related runtime data, and settings.

```mermaid
erDiagram
    LOCAL_DEVICE ||--o{ BUCKET : owns
    LOCAL_DEVICE ||--o{ DOCUMENT : owns
    LOCAL_DEVICE ||--o{ NOTE : owns
    LOCAL_DEVICE ||--o{ CONVERSATION : owns

    BUCKET ||--o{ DOCUMENT : groups
    BUCKET ||--o{ NOTE : groups

    DOCUMENT ||--o{ DOCUMENT_FILE : has
    DOCUMENT ||--o{ DOCUMENT_CHUNK : split_into
    DOCUMENT ||--o{ INGESTION_JOB : processed_by
    DOCUMENT ||--o{ SYNC_OUTBOX_ITEM : queues

    DOCUMENT_CHUNK ||--o| DOCUMENT_EMBEDDING : embedded_as

    NOTE ||--o{ NOTE_LINK : source
    NOTE ||--o{ NOTE_LINK : target
    NOTE ||--o{ SYNC_OUTBOX_ITEM : queues

    CONVERSATION ||--o{ CHAT_MESSAGE : contains

    APP_SETTING ||--o{ APP_SETTING : stores
    AI_MODEL ||--o{ AI_RUNTIME : used_by
    SYNC_STATE ||--o{ SYNC_OUTBOX_ITEM : tracks
```

## Main SQLite Tables

| Table | Purpose |
| --- | --- |
| `local_devices` | Local device identity and ownership boundary for offline-first data. |
| `buckets` | User-facing grouping/folder concept for documents and notes. |
| `documents` | Document metadata, status, sync status, ownership, and bucket relation. |
| `document_files` | Stored original file metadata, local path, file type, hash, and size. |
| `document_chunks` | Extracted text chunks used by search and RAG. |
| `document_embeddings` | Embedding vectors stored locally as BLOBs. |
| `ingestion_jobs` | Pending/processing/chunking/embedding/indexed/failed/cancelled document processing jobs with progress and sanitized diagnostics. |
| `notes` | Local markdown notes grouped by bucket. |
| `note_links` | Note-to-note links for future graph/backlink features. |
| `conversations` | Chat sessions. |
| `chat_messages` | Persisted user and assistant messages. |
| `sync_outbox` | Local-first sync operation queue. |
| `sync_state` | Sync cursor/state per scope. |
| `app_settings` | Runtime, AI, sync, and app settings. |
| `ai_models` | Local model registry/status. |

## Walking Skeleton

The walking skeleton is already implemented. It proves that the system can pass real data through all major layers.

### Document Upload Flow

```mermaid
sequenceDiagram
    actor User
    participant UI as React DocumentsPage
    participant API as LocalApi /api/documents/upload
    participant Handler as UploadDocumentHandler
    participant Storage as LocalFileStorageService
    participant DB as SQLite via EF Core

    User->>UI: Select a file
    UI->>API: POST multipart file
    API->>Handler: UploadDocumentCommand
    Handler->>DB: Resolve bucket or create Default
    Handler->>Storage: Save original file
    Storage-->>Handler: Stored file metadata
    Handler->>DB: Insert Document, DocumentFile, IngestionJob
    Handler-->>API: UploadDocumentResponse
    API-->>UI: 201 JSON response
    UI->>API: GET /api/documents
    API->>DB: Query documents page
    API-->>UI: CursorPage<DocumentDto>
    UI-->>User: Render uploaded document
```

Implemented code path:

- UI page: `apps/desktop/src/pages/DocumentsPage/index.tsx`
- Upload hook: `apps/desktop/src/features/document-upload/model/useDocumentUpload.ts`
- API client: `apps/desktop/src/shared/api/documents.ts`
- Endpoint: `backend/src/KnowledgeApp.LocalApi/Endpoints/Documents/DocumentEndpoints.cs`
- Handler: `backend/src/KnowledgeApp.Application/Documents/Commands/UploadDocumentHandler.cs`
- Bucket resolution: `backend/src/KnowledgeApp.Application/Buckets/Services/BucketResolver.cs`
- Local file storage: `backend/src/KnowledgeApp.Infrastructure/Services/Storage/LocalFileStorageService.cs`
- EF Core context: `backend/src/KnowledgeApp.Infrastructure/Persistence/AppDbContext.cs`

### Document Ingestion Flow

```mermaid
sequenceDiagram
    actor User
    participant UI as React DocumentsPage
    participant API as LocalApi
    participant Processor as IngestionJobProcessor
    participant Extractor as DocumentTextExtractor
    participant Chunker as DocumentChunker
    participant DB as SQLite

    User->>UI: Click Process
    UI->>API: POST /api/ingestion/jobs/{id}/process
    API->>Processor: Process job
    Processor->>DB: Load job, document, file
    Processor->>Extractor: Extract text
    Extractor-->>Processor: Text
    Processor->>Chunker: Split text
    Chunker-->>Processor: Chunks
    Processor->>DB: Save chunks and update statuses
    API-->>UI: ProcessIngestionJobResponse
    UI->>API: GET /api/documents
    UI-->>User: Render Indexed or Failed status
```

Implemented code path:

- Ingestion hook: `apps/desktop/src/features/document-ingestion/model/useProcessIngestionJob.ts`
- Ingestion endpoint: `backend/src/KnowledgeApp.LocalApi/Endpoints/Ingestion/IngestionEndpoints.cs`
- Handler: `backend/src/KnowledgeApp.Application/Ingestion/Commands/ProcessIngestionJobHandler.cs`
- Processor: `backend/src/KnowledgeApp.Infrastructure/Services/Ingestion/IngestionJobProcessor.cs`
- Extractors: `backend/src/KnowledgeApp.Infrastructure/Services/Ingestion/Extractors`
- Chunker: `backend/src/KnowledgeApp.Infrastructure/Services/Ingestion/SimpleDocumentChunker.cs`

### Notes Walking Skeleton

```mermaid
sequenceDiagram
    actor User
    participant UI as React Notes vault
    participant API as LocalApi /api/notes
    participant Handler as Notes handlers
    participant DB as SQLite

    User->>UI: Create markdown file
    UI->>API: POST /api/notes
    API->>Handler: CreateNoteRequest
    Handler->>DB: Insert Note
    API-->>UI: NoteDto JSON
    UI->>API: GET /api/notes
    API->>DB: Query notes with cursor pagination
    API-->>UI: CursorPage<NoteDto>
    UI-->>User: Render note in vault tree
```

Implemented code path:

- UI page: `apps/desktop/src/pages/NotesPage/index.tsx`
- Notes feature: `apps/desktop/src/features/note-editor`
- API client: `apps/desktop/src/shared/api/notes.ts`
- Endpoint: `backend/src/KnowledgeApp.LocalApi/Endpoints/Notes/NoteEndpoints.cs`
- Handlers: `backend/src/KnowledgeApp.Application/Notes`

## Verification Commands

The walking skeleton and project health can be verified with:

```powershell
pnpm install
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/check.ps1
```

Useful focused checks:

```powershell
dotnet build backend/KnowledgeApp.slnx --no-restore
dotnet test backend/KnowledgeApp.slnx --no-build
pnpm --filter desktop lint
pnpm --filter desktop typecheck
pnpm --filter desktop build
```

Manual local run:

```powershell
pnpm dev
```

Portable package:

```powershell
pnpm package
```

## Repository Integration

The walking skeleton code is already integrated into the shared repository. The latest portable release workflow also proves that the project can be built and packaged from GitHub Actions.

Relevant release workflow:

- `.github/workflows/check.yml`
- `.github/workflows/portable-release.yml`
