# Concept and MVP

## Product Concept

`localmind` is a portable offline-first desktop knowledge application for working with documents, notes, semantic search, and local AI chat. The application is designed for users who want to build a personal knowledge base from local files without depending on cloud services or a permanent internet connection.

The core problem: students, researchers, and knowledge workers often store information in disconnected PDFs, DOCX files, notes, and markdown documents. Finding exact information later is slow, and cloud AI tools may be unavailable, expensive, or unsuitable for private documents. `localmind` solves this by keeping files, indexes, search, notes, and AI-assisted answers on the user's machine.

## Target Users

- Students who collect lecture materials, PDFs, and personal notes.
- Researchers who need local search over documents.
- Knowledge workers who want a private offline knowledge base.
- Users who need a portable app that starts without manual backend or database setup.

## MVP Goal for 6 Weeks

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

## MVP Scope

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

## Out of Scope for MVP

The following items are intentionally outside the 6-week MVP:

- Full remote synchronization between devices.
- User accounts, login, registration, subscriptions, or cloud backup.
- Production-grade conflict resolution.
- Full local LLM packaging with large model files in the repository.
- OCR for scanned PDFs or images.
- Advanced semantic ranking with sqlite-vec or approximate vector indexes.
- Auto-update, installer signing, and production telemetry.
- Mobile or web versions of the app.

