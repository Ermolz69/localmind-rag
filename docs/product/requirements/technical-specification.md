# Technical Specification: localmind

## 1. Concept and MVP

`localmind` is a portable offline-first desktop knowledge application for local documents, notes, semantic search, and AI-assisted chat through RAG. The application is designed for users who want to build a private personal knowledge base without depending on cloud services or a permanent internet connection.

The core problem is that students, researchers, and knowledge workers often keep information in scattered PDFs, DOCX files, presentations, markdown files, text documents, and personal notes. Searching across them manually is slow, and cloud AI tools may be unavailable, expensive, or unsuitable for private documents. `localmind` solves this by keeping files, metadata, ingestion, indexes, notes, and AI workflows on the user's machine.

The key user scenario is simple: the user receives a portable folder or installer, launches `localmind.exe`, and the app works. The UI opens automatically, the local backend starts as a hidden sidecar process, SQLite is created automatically, documents can be uploaded without internet, and local ingestion prepares them for search and RAG.

### MVP Goal for 6 Weeks

The MVP must prove the local-first workflow end to end:

1. A user starts the desktop app without manually running Docker, PostgreSQL, backend services, migrations, or AI CLI tools.
2. The Tauri UI opens and connects to `KnowledgeApp.LocalApi`.
3. The local backend creates runtime folders and SQLite database automatically.
4. A user can create/select buckets and upload supported documents.
5. Documents are stored locally and queued for ingestion.
6. Text is extracted from `.txt`, `.md`, `.html`, `.pdf`, `.docx`, and `.pptx` files.
7. Documents are split into chunks with source metadata where possible.
8. Notes can be created and edited locally.
9. The UI shows runtime, document, ingestion, and error states clearly.
10. A portable Windows package can be built and launched without a development server.

### MVP Scope

The team commits to deliver:

- Desktop shell: Tauri + React application with navigation, layout, light/dark/system themes.
- Local backend: ASP.NET Core `LocalApi` sidecar with health/runtime endpoints.
- Local storage: SQLite database, local file storage, runtime folders, migrations.
- Buckets: basic bucket creation, selection, and document filtering.
- Documents: upload, metadata, local file saving, status tracking.
- Ingestion: extraction and chunking for text, markdown, HTML, PDF, DOCX, and PPTX.
- Notes: basic notes list, create, edit, delete, and bucket relation.
- Diagnostics: runtime status and ingestion error feedback in the UI.
- Quality gates: build, tests, formatting, linting, typecheck, GitHub Actions checks.
- Portable package: Windows portable build with app executable, backend sidecar, config, and runtime folders.

### Out of Scope for MVP

- Full remote synchronization between devices.
- User accounts, login, registration, subscriptions, or cloud backup.
- Production-grade conflict resolution.
- Full local LLM packaging with large model files in the repository.
- OCR for scanned PDFs or images.
- Advanced semantic ranking with sqlite-vec or approximate vector indexes.
- Auto-update, installer signing, and production telemetry.
- Mobile or web versions of the app.

## 2. Functional Requirements

Functional requirements are described as user stories and grouped by epics.

Priority levels:

- `Must`: required for MVP.
- `Should`: important, but can be reduced if time is limited.
- `Could`: useful after MVP stabilization.

### Epic 1: Desktop Startup and Local Runtime

| ID | Priority | User Story | Acceptance Criteria |
| --- | --- | --- | --- |
| US-01 | Must | As a user, I want to start `localmind` by launching one desktop executable, so that I do not need to run backend commands manually. | Tauri starts the UI, starts or connects to LocalApi, and the UI shows LocalApi connection status. |
| US-02 | Must | As a user, I want the app to create local runtime folders automatically, so that the first launch works without manual setup. | `runtime/app/data`, `runtime/app/files`, `runtime/app/indexes`, and `runtime/app/logs` are created automatically in portable mode. |
| US-03 | Must | As a user, I want the local SQLite database to be created and migrated automatically, so that I can use the app immediately. | On startup, SQLite exists and required tables are available without manual migrations. |
| US-04 | Should | As a user, I want to see runtime health and diagnostics, so that I understand whether local services are ready. | UI shows LocalApi status, AI runtime status, sync status, and key runtime paths. |

### Epic 2: Buckets and Document Organization

| ID | Priority | User Story | Acceptance Criteria |
| --- | --- | --- | --- |
| US-05 | Must | As a user, I want to create buckets, so that I can organize documents by topic. | User can create a bucket from the UI and see it in the bucket list. |
| US-06 | Must | As a user, I want uploaded documents to go into a selected bucket or a default bucket, so that no document is left unorganized. | Upload with selected bucket stores `Document.BucketId`; upload without selection resolves to last selected or `Default`. |
| US-07 | Must | As a user, I want to filter documents by bucket, so that I can focus on one knowledge area. | Documents page supports all-bucket view and selected-bucket filtering. |
| US-08 | Could | As a user, I want to rename or delete buckets, so that I can maintain my knowledge base over time. | Bucket edit/delete actions exist and preserve offline data consistency. |

### Epic 3: Document Upload and Ingestion

| ID | Priority | User Story | Acceptance Criteria |
| --- | --- | --- | --- |
| US-09 | Must | As a user, I want to upload local documents, so that they become part of my knowledge base. | UI supports file picker or drag-and-drop upload; backend saves original file locally. |
| US-10 | Must | As a user, I want the app to validate uploaded documents, so that unsupported or broken files fail clearly. | Upload validates file name, size, and extension; ingestion stores parsing errors in `IngestionJob.LastError`. |
| US-11 | Must | As a user, I want text to be extracted from common document formats, so that documents can be searched later. | `.txt`, `.md`, `.html`, `.pdf`, `.docx`, and `.pptx` are extracted by local extractors. |
| US-12 | Must | As a user, I want documents to be split into chunks, so that search and RAG can use relevant fragments. | Ingestion creates `DocumentChunk` records in stable order; reindexing replaces old chunks. |
| US-13 | Should | As a user, I want chunks to keep page or slide references where possible, so that answers can cite useful sources. | PDF chunks store page number; PPTX chunks store slide number; DOCX chunks keep document-level source metadata. |
| US-14 | Must | As a user, I want to see document processing status, so that I know whether a file is queued, processing, indexed, or failed. | UI displays `Queued`, `Processing`, `Indexed`, and `Failed` states and shows failure reason when available. |

### Epic 4: Notes

| ID | Priority | User Story | Acceptance Criteria |
| --- | --- | --- | --- |
| US-15 | Must | As a user, I want to create notes, so that I can store my own thoughts next to documents. | User can create a note with title and markdown content. |
| US-16 | Must | As a user, I want to edit and delete notes, so that my local knowledge base remains useful. | User can update note title/content and delete notes from the UI. |
| US-17 | Should | As a user, I want notes to belong to buckets, so that documents and notes share the same organization model. | Notes can be assigned to a bucket and filtered or displayed with bucket context. |
| US-18 | Could | As a user, I want to link notes to other notes or documents, so that I can build relationships between ideas. | Note links can be stored and displayed in a basic linked-notes view. |

### Epic 5: Local Search and RAG

| ID | Priority | User Story | Acceptance Criteria |
| --- | --- | --- | --- |
| US-19 | Should | As a user, I want semantic search over indexed documents, so that I can find information by meaning rather than exact words. | Search endpoint returns relevant chunks with document id, chunk id, score, page number, and snippet. |
| US-20 | Should | As a user, I want to ask questions over my local documents, so that I can get answers grounded in my own files. | Chat endpoint builds RAG context from local chunks and returns an answer with source references. |
| US-21 | Could | As a user, I want to choose or configure the local AI model, so that I can balance speed and answer quality. | Settings expose provider, model names, runtime path, model path, and basic runtime status. |

### Epic 6: Portable Packaging and Quality

| ID | Priority | User Story | Acceptance Criteria |
| --- | --- | --- | --- |
| US-22 | Must | As a user, I want a portable Windows package, so that I can run the app without installing developer tools. | Release artifact contains desktop executable, LocalApi sidecar, config, and runtime folders. |
| US-23 | Must | As a developer, I want CI checks to run automatically, so that broken commits are caught early. | GitHub Actions runs backend format/build/tests, frontend lint/typecheck/build, color guard, Docker compose validation, test reports, and coverage. |
| US-24 | Should | As a developer, I want architecture tests, so that project layers stay clean as the app grows. | Tests prevent Domain/Application from depending on Infrastructure and protect frontend slice boundaries where applicable. |

## 3. Non-Functional Requirements

### NFR-01: Offline-First Availability

The MVP must support local document upload, local metadata storage, document ingestion, notes, and viewing existing data without internet access.

Measurement:

- The application starts with network disabled.
- `/api/health` returns a successful local response.
- Uploading and ingesting a supported local document does not require remote services.
- Remote sync may be unavailable, but it must not block local workflows.

### NFR-02: Portable Startup

The Windows portable build must start without requiring a user to install Docker, PostgreSQL, Node.js, .NET SDK, Rust, Ollama, or run migrations manually.

Measurement:

- A tester extracts the portable zip and launches `localmind.exe`.
- The UI opens and LocalApi starts as a sidecar.
- SQLite database and runtime folders are created automatically.
- No manual CLI command is required after extraction.

### NFR-03: Performance for MVP-Scale Documents

The application must remain responsive for a small personal knowledge base.

Measurement:

- Uploading a file up to 100 MB is accepted or rejected with a clear validation error.
- Ingestion of a text-based document up to 5 MB completes without crashing the app.
- Document list loading for 1,000 document records completes in under 2 seconds on a typical development machine.
- UI must show loading or processing states for operations longer than 500 ms.

### NFR-04: Local Data Safety

The application must store user data predictably and avoid accidental data loss.

Measurement:

- Original uploaded files are stored under `runtime/app/files/{documentId}/{originalFileName}` in portable mode.
- SQLite database path is visible in diagnostics.
- Reindexing replaces chunks for one document and does not delete unrelated documents, notes, or files.
- Failed ingestion preserves the original file and stores the error message in `IngestionJob.LastError`.

### NFR-05: Maintainability and Quality Gates

The codebase must stay maintainable as a monorepo with frontend, backend, tests, and packaging.

Measurement:

- Backend builds with nullable reference types enabled and warnings as errors.
- GitHub Actions runs backend format/build/tests, frontend lint/typecheck/build, and Docker compose validation.
- Unit, integration, and architecture tests are separated in CI.
- Test reports and coverage artifacts are uploaded in GitHub Actions.
- Frontend components must use semantic theme tokens and pass the hardcoded color guard.

## 4. Priorities and Risks

### Critical for MVP

These user stories must be delivered first because they prove the main local-first value:

- US-01: Launch one desktop executable.
- US-02: Create local runtime folders automatically.
- US-03: Create and migrate SQLite automatically.
- US-05: Create buckets.
- US-06: Resolve selected/default bucket on upload.
- US-07: Filter documents by bucket.
- US-09: Upload local documents.
- US-10: Validate uploaded documents and store ingestion errors.
- US-11: Extract text from supported formats.
- US-12: Split documents into chunks.
- US-14: Show document processing status.
- US-15: Create notes.
- US-16: Edit and delete notes.
- US-22: Build portable Windows package.
- US-23: Run CI checks.

### Important if Time Allows

These stories make the app more useful but can be reduced if the MVP is at risk:

- US-04: Runtime health and diagnostics.
- US-13: Page and slide references for chunks.
- US-17: Bucket relation for notes.
- US-19: Semantic search.
- US-20: Local RAG chat.
- US-24: Architecture tests.

### Post-MVP

These stories are useful but not required for the 6-week MVP:

- US-08: Rename or delete buckets.
- US-18: Link notes to notes or documents.
- US-21: Advanced AI model configuration.
- Remote sync, cloud backup, accounts, and conflict resolution.
- OCR and scanned document support.
- Auto-update and installer signing.

### Risk 1: Local AI Runtime Packaging Is Complex

Description:

Packing a real local AI runtime and models can make the portable artifact large and fragile. Different machines may have different CPU/GPU capabilities.

Impact:

- AI chat may be delayed or unstable in the MVP.
- Release artifact may become too large for practical testing.

Mitigation:

- Keep AI runtime behind interfaces and settings.
- For MVP, prioritize document ingestion, chunking, and API/UI flow.
- Use a stub or external dev runtime until packaging is stable.
- Document where llama.cpp/Ollama integration will connect later.

### Risk 2: Document Parsing Quality Varies by File Type

Description:

PDF, DOCX, and PPTX files can contain images, scanned pages, unusual layouts, unsupported encodings, or corrupted package structures.

Impact:

- Some uploaded files may fail ingestion or produce low-quality chunks.
- RAG answers may have weak source context if text extraction is poor.

Mitigation:

- Start with text-based extraction only.
- Store parsing errors in `IngestionJob.LastError`.
- Keep original files safely stored for future reindexing.
- Add extractor unit tests and integration smoke tests.
- Defer OCR and advanced layout reconstruction to post-MVP.

### Risk 3: Scope Creep Around Sync and Cloud Features

Description:

The architecture includes optional remote sync, but implementing accounts, devices, conflict resolution, and remote storage is too large for the MVP.

Impact:

- The team may spend time on cloud features before the local app is useful.
- Core offline-first workflows may remain incomplete.

Mitigation:

- Treat `SyncApi` as skeleton-level during MVP.
- Implement local `sync_outbox` structures only where they do not block local workflows.
- Prioritize portable startup, local documents, notes, ingestion, and UI feedback.
- Move remote sync to a dedicated post-MVP milestone.

