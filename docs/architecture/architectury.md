# Knowledge App Architecture

## Overall architecture

```mermaid
flowchart TB
    User[User] --> Desktop[KnowledgeApp.exe]

    subgraph LocalMachine[Local Machine / Portable Runtime]
        Desktop --> Tauri[Tauri v2 + React UI]
        Tauri --> LocalApi[KnowledgeApp.LocalApi sidecar]

        LocalApi --> LocalDb[(SQLite local DB)]
        LocalApi --> FileStorage[Local file storage]
        LocalApi --> VectorIndex[Local vector index]
        LocalApi --> AiRuntime[Local AI Runtime]

        AiRuntime --> ChatModel[Chat model]
        AiRuntime --> EmbeddingModel[Embedding model]

        LocalApi --> IngestionWorker[Ingestion hosted worker]
        LocalApi --> SyncWorker[Sync hosted worker]
    end

    subgraph RemoteCloud[Optional Remote Sync]
        SyncApi[KnowledgeApp.SyncApi]
        RemoteDb[(PostgreSQL)]
        RemoteFiles[Remote file storage]
    end

    SyncWorker -. internet available .-> SyncApi
    SyncApi --> RemoteDb
    SyncApi --> RemoteFiles
```

## Offline mode

```mermaid
sequenceDiagram
    actor User
    participant UI as Tauri React UI
    participant API as LocalApi
    participant DB as SQLite
    participant FS as Local File Storage
    participant AI as Local AI Runtime
    participant IDX as Local Vector Index

    User->>UI: Upload document
    UI->>API: POST /api/documents
    API->>FS: Save original file
    API->>DB: Create Document + IngestionJob
    API->>API: Extract text
    API->>API: Split into chunks
    API->>AI: Generate embeddings
    AI-->>API: Embedding vectors
    API->>DB: Save chunks
    API->>IDX: Save vectors
    API-->>UI: Document indexed

    User->>UI: Ask question
    UI->>API: POST /api/chats/{id}/messages
    API->>AI: Embed question
    AI-->>API: Question vector
    API->>IDX: Semantic search
    IDX-->>API: Relevant chunks
    API->>AI: Generate answer with context
    AI-->>API: Answer
    API->>DB: Save chat message
    API-->>UI: Answer with sources
```

## Online sync

```mermaid
sequenceDiagram
    actor User
    participant Local as LocalApi
    participant Outbox as SyncOutbox
    participant Remote as SyncApi
    participant RDB as PostgreSQL
    participant RFS as Remote File Storage

    User->>Local: Work offline
    Local->>Outbox: Queue local changes

    Note over Local,Remote: Internet becomes available

    Local->>Remote: Authenticate device
    Remote-->>Local: Sync token

    Local->>Outbox: Read pending operations
    Local->>Remote: Upload metadata
    Remote->>RDB: Save metadata
    Local->>Remote: Upload files
    Remote->>RFS: Save files

    Local->>Remote: Request remote manifest
    Remote->>RDB: Load changes
    Remote-->>Local: Remote changes

    Local->>Local: Download missing files
    Local->>Local: Create local ingestion jobs
    Local->>Outbox: Mark operations synced
```

## Application startup lifecycle

```mermaid
flowchart TD
    A[User starts KnowledgeApp.exe] --> B[Tauri starts]
    B --> C{LocalApi running?}
    C -- No --> D[Start LocalApi sidecar]
    C -- Yes --> E[Connect to LocalApi]
    D --> E

    E --> F[Create app folders]
    F --> G[Open/Create SQLite DB]
    G --> H[Apply local migrations]
    H --> I{AI runtime configured?}

    I -- No --> J[Show setup screen]
    I -- Yes --> K{AI runtime running?}

    K -- No --> L[Start AI runtime sidecar]
    K -- Yes --> M[Check models]
    L --> M

    M --> N{Models available?}
    N -- No --> O[Show models missing state]
    N -- Yes --> P[Local RAG ready]

    P --> Q{Internet available?}
    Q -- No --> R[Offline mode]
    Q -- Yes --> S[Enable sync worker]
```

## Backend Clean Architecture dependencies

```mermaid
flowchart TB
    LocalApi[KnowledgeApp.LocalApi] --> Application[KnowledgeApp.Application]
    LocalApi --> Contracts[KnowledgeApp.Contracts]
    LocalApi --> Infrastructure[KnowledgeApp.Infrastructure]
    LocalApi --> Bootstrap[KnowledgeApp.Bootstrap]

    SyncApi[KnowledgeApp.SyncApi] --> Application
    SyncApi --> Contracts
    SyncApi --> Infrastructure
    SyncApi --> Bootstrap

    Worker[KnowledgeApp.Worker] --> Application
    Worker --> Infrastructure

    Infrastructure --> Application
    Infrastructure --> Domain[KnowledgeApp.Domain]
    Infrastructure --> Contracts

    Application --> Domain
    Application --> Contracts

    Contracts -. no infra deps .-> Domain

    Domain -. no dependencies .-> Domain
```

## RAG pipeline

```mermaid
flowchart LR
    Upload[Upload document] --> SaveFile[Save original file]
    SaveFile --> Job[Create ingestion job]
    Job --> Extract[Extract text]
    Extract --> Chunk[Split into chunks]
    Chunk --> Embed[Generate embeddings locally]
    Embed --> Store[Store chunks + vectors]
    Store --> SearchReady[Semantic search ready]

    Question[User question] --> EmbedQuestion[Embed question locally]
    EmbedQuestion --> VectorSearch[Vector search]
    VectorSearch --> Context[Build RAG context]
    Context --> Chat[Call local chat model]
    Chat --> Answer[Answer with sources]
```

## Data ownership

```mermaid
erDiagram
    LOCAL_DEVICE ||--o{ LOCAL_DOCUMENT : owns
    LOCAL_DOCUMENT ||--o{ LOCAL_DOCUMENT_FILE : has
    LOCAL_DOCUMENT ||--o{ LOCAL_DOCUMENT_CHUNK : split_into
    LOCAL_DOCUMENT_CHUNK ||--|| LOCAL_EMBEDDING : has
    LOCAL_DOCUMENT ||--o{ SYNC_OUTBOX_ITEM : queues

    REMOTE_USER ||--o{ REMOTE_DEVICE : owns
    REMOTE_USER ||--o{ REMOTE_DOCUMENT : owns
    REMOTE_DOCUMENT ||--o{ REMOTE_FILE : stores
    REMOTE_DOCUMENT ||--o{ REMOTE_SYNC_EVENT : produces
```

## Frontend feature-sliced structure

```mermaid
flowchart TB
    App[app] --> Pages[pages]
    Pages --> Widgets[widgets]
    Widgets --> Features[features]
    Features --> Entities[entities]
    Entities --> Shared[shared]

    Shared --> SharedUi[shared/ui]
    Shared --> SharedApi[shared/api]
    Shared --> SharedTheme[shared/theme]
    Shared --> SharedLib[shared/lib]

    Features -. must not import pages .-> Features
    Shared -. must not import features .-> Shared
```

## Sync state machine

```mermaid
stateDiagram-v2
    [*] --> LocalOnly
    LocalOnly --> PendingUpload
    PendingUpload --> Uploading
    Uploading --> Synced
    Uploading --> UploadFailed
    UploadFailed --> PendingUpload

    Synced --> PendingUpload: local change
    Synced --> PendingDownload: remote change
    PendingDownload --> Downloading
    Downloading --> Synced
    Downloading --> DownloadFailed
    DownloadFailed --> PendingDownload

    Synced --> Conflict
    Conflict --> Synced

    Synced --> DeletedLocal
    Synced --> DeletedRemote
    DeletedLocal --> PendingUpload
    DeletedRemote --> PendingDownload
```
