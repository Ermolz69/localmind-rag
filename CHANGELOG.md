# Changelog

All notable changes to `localmind-rag` are documented here.

## [0.1.0] - Base Architecture

This is the first foundation release of `localmind`. It does not claim a finished product UI or full local AI/RAG runtime yet. It establishes the project architecture, repository rules, checks, and portable-preview packaging path.

### Added

- Production-oriented monorepo structure for a Tauri + React desktop app and .NET backend.
- `KnowledgeApp.slnx` with LocalApi, SyncApi, Worker, Domain, Application, Infrastructure, Contracts, Bootstrap, and test projects.
- Clean Architecture project boundaries and architecture tests.
- Domain model skeleton for documents, chunks, embeddings, notes, chats, sync outbox, devices, settings, AI models, and AI runtime state.
- Application ports for storage, ingestion, chunking, embeddings, vector search, RAG, sync, runtime management, paths, locks, users, and time.
- SQLite `AppDbContext` with initial EF Core migration.
- LocalApi skeleton endpoints for health, runtime, buckets, documents, notes, chats, search, settings, and sync.
- React/Vite desktop scaffold with feature-sliced structure, semantic theme tokens, typed LocalApi client, sidebar, topbar, pages, and theme provider.
- Tauri v2 configuration scaffold with LocalApi sidecar placeholder.
- Runtime folder skeleton for portable app data, files, indexes, logs, AI runtime binaries, and models.
- Remote sync infrastructure scaffold with PostgreSQL/pgvector Docker Compose.
- CI workflows for focused check jobs and portable-preview release artifacts.
- Repository hygiene rules: `.gitignore`, `.dockerignore`, module-level ignores, and documentation.
- Architecture documentation and Mermaid diagrams.

### Verified

- Backend restore, build, and tests pass.
- Backend formatting passes.
- Frontend lint, typecheck, formatting, and build pass.
- Hardcoded color guard passes.
- Docker Compose config validation passes.
- Portable-preview packaging script creates a Windows x64 artifact folder.

### Known Limits

- Full `KnowledgeApp.exe` native Tauri bundle is not wired into the portable artifact yet.
- Local AI runtime adapters are interface/stub level.
- RAG ingestion, parsing, embeddings, and vector search are scaffolded, not production-complete.
- Remote sync API is endpoint skeleton only.
- LocalApi endpoints still contain some skeleton logic that should move into Application use cases.
