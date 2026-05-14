# Non-Functional Requirements

## NFR-01: Offline-First Availability

The MVP must support local document upload, local metadata storage, document ingestion, notes, and viewing existing data without internet access.

Measurement:

- The application starts with network disabled.
- `/api/health` returns a successful local response.
- Uploading and ingesting a supported local document does not require remote services.
- Remote sync may be unavailable, but it must not block local workflows.

## NFR-02: Portable Startup

The Windows portable build must start without requiring a user to install Docker, PostgreSQL, Node.js, .NET SDK, Rust, Ollama, or run migrations manually.

Measurement:

- A tester extracts the portable zip and launches `localmind.exe`.
- The UI opens and LocalApi starts as a sidecar.
- SQLite database and runtime folders are created automatically.
- No manual CLI command is required after extraction.

## NFR-03: Performance for MVP-Scale Documents

The application must remain responsive for a small personal knowledge base.

Measurement:

- Uploading a file up to 100 MB is accepted or rejected with a clear validation error.
- Ingestion of a text-based document up to 5 MB completes without crashing the app.
- Document list loading for 1,000 document records completes in under 2 seconds on a typical development machine.
- UI must show loading or processing states for operations longer than 500 ms.

## NFR-04: Local Data Safety

The application must store user data predictably and avoid accidental data loss.

Measurement:

- Original uploaded files are stored under `runtime/app/files/{documentId}/{originalFileName}` in portable mode.
- SQLite database path is visible in diagnostics.
- Reindexing replaces chunks for one document and does not delete unrelated documents, notes, or files.
- Failed ingestion preserves the original file and stores the error message in `IngestionJob.LastError`.

## NFR-05: Maintainability and Quality Gates

The codebase must stay maintainable as a monorepo with frontend, backend, tests, and packaging.

Measurement:

- Backend builds with nullable reference types enabled and warnings as errors.
- GitHub Actions runs backend format/build/tests, frontend lint/typecheck/build, and Docker compose validation.
- Unit, integration, and architecture tests are separated in CI.
- Test reports and coverage artifacts are uploaded in GitHub Actions.
- Frontend components must use semantic theme tokens and pass the hardcoded color guard.

