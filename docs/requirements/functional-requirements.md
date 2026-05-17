# Functional Requirements

Functional requirements are described as user stories and grouped by epics.

Priority levels:

- `Must`: required for MVP.
- `Should`: important, but can be reduced if time is limited.
- `Could`: useful after MVP stabilization.

## Epic 1: Desktop Startup and Local Runtime

| ID | Priority | User Story | Acceptance Criteria |
| --- | --- | --- | --- |
| US-01 | Must | As a user, I want to start `localmind` by launching one desktop executable, so that I do not need to run backend commands manually. | Tauri starts the UI, starts or connects to LocalApi, and the UI shows LocalApi connection status. |
| US-02 | Must | As a user, I want the app to create local runtime folders automatically, so that the first launch works without manual setup. | `runtime/app/data`, `runtime/app/files`, `runtime/app/indexes`, and `runtime/app/logs` are created automatically in portable mode. |
| US-03 | Must | As a user, I want the local SQLite database to be created and migrated automatically, so that I can use the app immediately. | On startup, SQLite exists and required tables are available without manual migrations. |
| US-04 | Should | As a user, I want to see runtime health and diagnostics, so that I understand whether local services are ready. | UI shows LocalApi status, AI runtime status, sync status, and key runtime paths. |

## Epic 2: Buckets and Document Organization

| ID | Priority | User Story | Acceptance Criteria |
| --- | --- | --- | --- |
| US-05 | Must | As a user, I want to create buckets, so that I can organize documents by topic. | User can create a bucket from the UI and see it in the bucket list. |
| US-06 | Must | As a user, I want uploaded documents to go into a selected bucket or a default bucket, so that no document is left unorganized. | Upload with selected bucket stores `Document.BucketId`; upload without selection resolves to last selected or `Default`. |
| US-07 | Must | As a user, I want to filter documents by bucket, so that I can focus on one knowledge area. | Documents page supports all-bucket view and selected-bucket filtering. |
| US-08 | Could | As a user, I want to rename or delete buckets, so that I can maintain my knowledge base over time. | Bucket edit/delete actions exist and preserve offline data consistency. |

## Epic 3: Document Upload and Ingestion

| ID | Priority | User Story | Acceptance Criteria |
| --- | --- | --- | --- |
| US-09 | Must | As a user, I want to upload local documents, so that they become part of my knowledge base. | UI supports file picker or drag-and-drop upload; backend saves original file locally. |
| US-10 | Must | As a user, I want the app to validate uploaded documents, so that unsupported or broken files fail clearly. | Upload validates file name, size, and extension; ingestion stores parsing errors in `IngestionJob.LastError`. |
| US-11 | Must | As a user, I want text to be extracted from common document formats, so that documents can be searched later. | `.txt`, `.md`, `.html`, `.pdf`, `.docx`, and `.pptx` are extracted by local extractors. |
| US-12 | Must | As a user, I want documents to be split into chunks, so that search and RAG can use relevant fragments. | Ingestion creates `DocumentChunk` records in stable order; reindexing replaces old chunks. |
| US-13 | Should | As a user, I want chunks to keep page or slide references where possible, so that answers can cite useful sources. | PDF chunks store page number; PPTX chunks store slide number; DOCX chunks keep document-level source metadata. |
| US-14 | Must | As a user, I want to see document processing status, so that I know whether a file is queued, processing, indexed, or failed. | UI displays `Queued`, `Processing`, `Indexed`, and `Failed` states and shows failure reason when available. |

## Epic 4: Notes

| ID | Priority | User Story | Acceptance Criteria |
| --- | --- | --- | --- |
| US-15 | Must | As a user, I want to create notes, so that I can store my own thoughts next to documents. | User can create a note with title and markdown content. |
| US-16 | Must | As a user, I want to edit and delete notes, so that my local knowledge base remains useful. | User can update note title/content and delete notes from the UI. |
| US-17 | Should | As a user, I want notes to belong to buckets, so that documents and notes share the same organization model. | Notes can be assigned to a bucket and filtered or displayed with bucket context. |
| US-18 | Could | As a user, I want to link notes to other notes or documents, so that I can build relationships between ideas. | Note links can be stored and displayed in a basic linked-notes view. |

## Epic 5: Local Search and RAG

| ID | Priority | User Story | Acceptance Criteria |
| --- | --- | --- | --- |
| US-19 | Should | As a user, I want semantic search over indexed documents, so that I can find information by meaning rather than exact words. | Search endpoint returns relevant chunks with document id, chunk id, score, page number, and snippet. |
| US-20 | Should | As a user, I want to ask questions over my local documents, so that I can get answers grounded in my own files. | Chat endpoint builds RAG context from local chunks and returns an answer with source references. |
| US-21 | Could | As a user, I want to choose or configure the local AI model, so that I can balance speed and answer quality. | Settings expose provider, model names, runtime path, model path, and basic runtime status. |

## Epic 6: Portable Packaging and Quality

| ID | Priority | User Story | Acceptance Criteria |
| --- | --- | --- | --- |
| US-22 | Must | As a user, I want a portable Windows package, so that I can run the app without installing developer tools. | Release artifact contains desktop executable, LocalApi sidecar, config, and runtime folders. |
| US-23 | Must | As a developer, I want CI checks to run automatically, so that broken commits are caught early. | GitHub Actions runs backend format/build/tests, frontend lint/typecheck/build, color guard, Docker compose validation, test reports, and coverage. |
| US-24 | Should | As a developer, I want architecture tests, so that project layers stay clean as the app grows. | Tests prevent Domain/Application from depending on Infrastructure and protect frontend slice boundaries where applicable. |

