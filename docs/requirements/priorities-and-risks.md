# Priorities and Risks

## MVP Priority Groups

## Critical for MVP

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

## Important if Time Allows

These stories make the app more useful but can be reduced if the MVP is at risk:

- US-04: Runtime health and diagnostics.
- US-13: Page and slide references for chunks.
- US-17: Bucket relation for notes.
- US-19: Semantic search.
- US-20: Local RAG chat.
- US-24: Architecture tests.

## Post-MVP

These stories are useful but not required for the 6-week MVP:

- US-08: Rename or delete buckets.
- US-18: Link notes to notes or documents.
- US-21: Advanced AI model configuration.
- Remote sync, cloud backup, accounts, and conflict resolution.
- OCR and scanned document support.
- Auto-update and installer signing.

## Risks

## Risk 1: Local AI Runtime Packaging Is Complex

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

## Risk 2: Document Parsing Quality Varies by File Type

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

## Risk 3: Scope Creep Around Sync and Cloud Features

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

