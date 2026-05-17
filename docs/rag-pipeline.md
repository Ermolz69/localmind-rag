# RAG Pipeline

Localmind keeps the document flow local-first. Upload creates durable local metadata first, stores the original file under the document id, queues ingestion, and only then later parsing/chunking/embedding workers make semantic search available.

## Upload slice

```mermaid
sequenceDiagram
    actor User
    participant UI as "Tauri React UI"
    participant API as "LocalApi"
    participant Handler as "UploadDocumentHandler"
    participant Buckets as "BucketResolver"
    participant Storage as "LocalFileStorageService"
    participant DB as "SQLite"

    User->>UI: Upload document
    UI->>API: POST /api/documents/upload
    API->>Handler: UploadDocumentCommand
    Handler->>Handler: Validate file size/name/extension
    Handler->>Buckets: Resolve bucket
    Buckets->>DB: Read requested or last selected bucket
    alt No bucket available
        Buckets->>DB: Create Default bucket
    end
    Buckets-->>Handler: Bucket
    Handler->>Storage: Save runtime/app/files/{documentId}/{fileName}
    Storage-->>Handler: StoredFileDto + SHA-256
    Handler->>DB: Add Document + DocumentFile + IngestionJob
    Handler->>DB: SaveChanges
    Handler-->>API: UploadDocumentResponse
    API-->>UI: 201 Created
```

Rules:

- `Document.BucketId` is always assigned for new uploads.
- Explicit missing `bucketId` is an error.
- Missing optional `bucketId` resolves to the last selected bucket, then to the system `Default` bucket.
- Files are stored predictably in `runtime/app/files/{documentId}/{originalFileName}`.

## Full RAG flow

```mermaid
flowchart LR
    Upload["Upload document"] --> ResolveBucket["Resolve bucket"]
    ResolveBucket --> SaveFile["Save original file"]
    SaveFile --> Job["Create ingestion job"]
    Job --> Extract["Extract text"]
    Extract --> Chunk["Split into chunks"]
    Chunk --> Embed["Generate embeddings locally"]
    Embed --> Store["Store chunks and vectors"]
    Store --> SearchReady["Semantic search ready"]

    Question["User question"] --> EmbedQuestion["Embed question locally"]
    EmbedQuestion --> VectorSearch["Vector search"]
    VectorSearch --> Context["Build RAG context"]
    Context --> Chat["Call local chat model"]
    Chat --> Answer["Answer with sources"]
```

## Ingestion MVP

```mermaid
sequenceDiagram
    participant Processor as "IngestionJobProcessor"
    participant DB as "SQLite"
    participant Extractor as "DocumentTextExtractor"
    participant Chunker as "DocumentChunker"

    Processor->>DB: Load queued IngestionJob
    Processor->>DB: Load Document + DocumentFile
    Processor->>DB: Mark job Running and document Processing
    Processor->>Extractor: Extract text from local file
    alt Supported text/markdown/html/pdf/docx/pptx
        Extractor-->>Processor: Text segments with source metadata
        Processor->>Chunker: Split into chunks
        Chunker-->>Processor: Chunk texts
        Processor->>DB: Replace DocumentChunk rows with page/slide metadata where available
        Processor->>DB: Mark job Completed and document Indexed
    else Corrupt or unsupported file
        Extractor-->>Processor: Extraction exception
        Processor->>DB: Mark job Failed and document Failed
    end
```

Current MVP support:

- `.txt`, `.md`, `.markdown` are extracted as raw text.
- `.html`, `.htm` are extracted by stripping scripts, styles and tags, then decoding HTML entities.
- `.pdf` is extracted page by page and stores page numbers on chunks.
- `.docx` is extracted from document paragraphs.
- `.pptx` is extracted slide by slide and stores slide numbers on chunks.
- Corrupt files fail ingestion with the parsing error stored in `IngestionJob.LastError`.
