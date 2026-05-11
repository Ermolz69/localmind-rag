# localmind Architecture Diagrams

This file is a visual map of the current `localmind-rag` scaffold. The diagrams describe what exists now and where the next implementation stages should plug in.

## Monorepo Map

```mermaid
flowchart TB
    Repo["localmind-rag"]

    Repo --> Desktop["apps/desktop<br/>Tauri + React UI"]
    Repo --> Backend["backend<br/>.NET solution"]
    Repo --> Runtime["runtime<br/>portable data + AI assets"]
    Repo --> Infra["infra<br/>remote sync dependencies"]
    Repo --> Docs["docs<br/>architecture notes"]
    Repo --> Scripts["scripts<br/>setup/check/dev/package"]
    Repo --> CI[".github/workflows<br/>CI gates"]

    Desktop --> DesktopSrc["src<br/>FSD frontend"]
    Desktop --> TauriCfg["src-tauri<br/>sidecar config"]

    Backend --> LocalApi["KnowledgeApp.LocalApi"]
    Backend --> SyncApi["KnowledgeApp.SyncApi"]
    Backend --> Worker["KnowledgeApp.Worker"]
    Backend --> Domain["KnowledgeApp.Domain"]
    Backend --> Application["KnowledgeApp.Application"]
    Backend --> Infrastructure["KnowledgeApp.Infrastructure"]
    Backend --> Contracts["KnowledgeApp.Contracts"]
    Backend --> Bootstrap["KnowledgeApp.Bootstrap"]
    Backend --> Tests["Unit / Integration / Architecture tests"]

    Runtime --> AppRuntime["runtime/app<br/>data files indexes logs"]
    Runtime --> AiRuntime["runtime/ai<br/>bin models"]
```

## Desktop Runtime Boundary

```mermaid
flowchart LR
    UI["React UI"] --> ApiClient["typed LocalApi client"]
    ApiClient --> LocalApi["KnowledgeApp.LocalApi"]

    subgraph Forbidden["Forbidden direct access"]
        SQLite[(SQLite)]
        Ollama["Ollama / llama.cpp"]
        Files["Local files"]
        Remote["Remote SyncApi"]
    end

    UI -. must not call .-> SQLite
    UI -. must not call .-> Ollama
    UI -. must not read directly .-> Files
    UI -. normally hidden behind LocalApi .-> Remote

    LocalApi --> SQLite
    LocalApi --> Files
    LocalApi --> Ollama
    LocalApi -. optional sync .-> Remote
```

## Backend Project Dependencies

```mermaid
flowchart BT
    Domain["Domain<br/>entities enums value objects"]
    Contracts["Contracts<br/>DTOs API models"]
    Application["Application<br/>ports use cases orchestration"]
    Infrastructure["Infrastructure<br/>EF Core SQLite adapters services"]
    Bootstrap["Bootstrap<br/>DI logging problem details"]
    LocalApi["LocalApi<br/>desktop HTTP boundary"]
    SyncApi["SyncApi<br/>remote sync HTTP boundary"]
    Worker["Worker<br/>background jobs"]

    Contracts --> Domain
    Application --> Domain
    Application --> Contracts
    Infrastructure --> Application
    Infrastructure --> Domain
    Infrastructure --> Contracts
    Bootstrap --> Application
    Bootstrap --> Infrastructure
    Bootstrap --> Contracts
    LocalApi --> Bootstrap
    LocalApi --> Application
    LocalApi --> Infrastructure
    LocalApi --> Contracts
    SyncApi --> Bootstrap
    SyncApi --> Application
    SyncApi --> Infrastructure
    SyncApi --> Contracts
    Worker --> Application
    Worker --> Infrastructure
    Worker --> Bootstrap
```

## LocalApi Startup

```mermaid
sequenceDiagram
    participant Tauri as Tauri shell
    participant API as KnowledgeApp.LocalApi
    participant Paths as AppPathProvider
    participant DB as SQLite
    participant AI as AI runtime adapter

    Tauri->>API: Start sidecar process
    API->>Paths: Resolve root and runtime folders
    Paths-->>API: runtime/app paths
    API->>API: Create data/files/indexes/logs folders
    API->>DB: Apply EF Core migrations
    API->>AI: Read AI settings and model paths
    AI-->>API: Runtime status
    API-->>Tauri: /api/health OK
```

## Local SQLite Schema

```mermaid
erDiagram
    BUCKETS ||--o{ DOCUMENTS : contains
    BUCKETS ||--o{ NOTES : contains
    DOCUMENTS ||--o{ DOCUMENT_FILES : stores
    DOCUMENTS ||--o{ DOCUMENT_CHUNKS : splits_into
    DOCUMENT_CHUNKS ||--|| DOCUMENT_EMBEDDINGS : embeds
    DOCUMENTS ||--o{ INGESTION_JOBS : schedules
    CONVERSATIONS ||--o{ CHAT_MESSAGES : has
    NOTES ||--o{ NOTE_LINKS : source
    NOTES ||--o{ NOTE_LINKS : target
    DOCUMENTS ||--o{ SYNC_OUTBOX : queues
    NOTES ||--o{ SYNC_OUTBOX : queues
    BUCKETS ||--o{ SYNC_OUTBOX : queues
    SYNC_STATE ||--o{ SYNC_OUTBOX : tracks
    APP_SETTINGS ||--o{ AI_MODELS : configures
```

## RAG Components

```mermaid
flowchart TB
    Upload["Document upload endpoint"] --> Storage["IFileStorageService"]
    Storage --> DocumentRecord["Document + DocumentFile"]
    DocumentRecord --> Job["IngestionJob"]

    Job --> ExtractorFactory["IDocumentTextExtractorFactory"]
    ExtractorFactory --> Extractor["Pdf / Docx / Markdown / Text extractors"]
    Extractor --> Chunker["IDocumentChunker"]
    Chunker --> Embeddings["IEmbeddingGenerator"]
    Embeddings --> VectorIndex["IVectorIndex"]
    VectorIndex --> Search["IVectorSearchService"]

    Question["User question"] --> RagContext["IRagContextBuilder"]
    RagContext --> Search
    Search --> Sources["RagSourceDto[]"]
    Sources --> Answer["IRagAnswerGenerator"]
    Answer --> ChatModel["IChatModelClient"]
    ChatModel --> Response["Answer with sources"]
```

## Frontend Feature Slices

```mermaid
flowchart TB
    App["app<br/>router providers styles"] --> Pages["pages"]
    Pages --> Widgets["widgets"]
    Widgets --> Features["features"]
    Features --> Entities["entities"]
    Entities --> Shared["shared"]

    Shared --> Api["shared/api"]
    Shared --> Ui["shared/ui"]
    Shared --> Theme["shared/theme"]
    Shared --> Lib["shared/lib"]

    subgraph Rules["Import rules"]
        R1["shared must not import features"]
        R2["features must not import pages"]
        R3["pages compose widgets/features/entities"]
        R4["UI calls LocalApi through shared/api"]
    end
```

## Development Flow

```mermaid
flowchart LR
    Dev["pnpm dev"] --> Concurrent["concurrently"]
    Concurrent --> LocalApi["dotnet run LocalApi<br/>127.0.0.1:49321"]
    Concurrent --> Vite["vite dev server<br/>127.0.0.1:5173"]
    Vite --> React["React UI"]
    React --> LocalApi
    LocalApi --> SQLite[(runtime/app/data/knowledge-app.db)]
```

## Quality Gates

```mermaid
flowchart TB
    Check["pnpm check / scripts/check.ps1"]
    Check --> Restore["dotnet restore"]
    Check --> Build["dotnet build"]
    Check --> Test["dotnet test"]
    Check --> Lint["desktop ESLint"]
    Check --> Typecheck["desktop TypeScript strict"]
    Check --> Format["desktop Prettier check"]

    Build --> BackendRules["nullable + warnings as errors<br/>except current NuGet audit warning"]
    Lint --> FrontendRules["no unnecessary any<br/>React hooks lint"]
    Typecheck --> ApiContracts["typed LocalApi client"]
```

## Portable Packaging Target

```mermaid
flowchart TB
    Package["pnpm package"] --> PublishApi["dotnet publish LocalApi self-contained"]
    Package --> BuildUi["desktop build"]
    Package --> Bundle["Tauri bundle"]

    Bundle --> Folder["KnowledgeApp portable folder"]
    Folder --> Exe["KnowledgeApp.exe"]
    Folder --> Bin["bin<br/>KnowledgeApp.LocalApi.exe<br/>llama-server.exe"]
    Folder --> Runtime["runtime<br/>app data files indexes logs<br/>ai models"]
    Folder --> Config["config/appsettings.json"]

    Exe --> Bin
    Bin --> Runtime
    Bin --> Config
```

## Remote Sync Ownership

```mermaid
flowchart LR
    LocalDevice["Local device"] --> LocalDb[(SQLite)]
    LocalDevice --> LocalFiles["local files"]
    LocalDevice --> LocalEmbeddings["local embeddings"]

    LocalDb --> Outbox["sync_outbox"]
    Outbox --> SyncWorker["sync worker"]
    SyncWorker -. authenticated + online .-> SyncApi["KnowledgeApp.SyncApi"]

    SyncApi --> RemoteDb[(PostgreSQL)]
    SyncApi --> RemoteFiles["remote file storage"]

    LocalEmbeddings -. not synced by default .- RemoteDb
```
