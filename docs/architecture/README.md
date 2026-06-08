# Architecture

LocalMind is a local-first desktop system: a Tauri React UI talks to a loopback-only ASP.NET Core `KnowledgeApp.LocalApi` sidecar, which owns SQLite persistence, local file storage, ingestion, vector search, RAG chat, diagnostics, and AI runtime provider adapters.

This section describes the system as it is implemented now. It avoids future-tense design notes unless a feature is explicitly out of scope.

## Reading Path

1. [System overview](./system.md) explains the runtime shape and project boundaries.
2. [Backend architecture](./backend.md) explains the modular monolith, feature folders, application ports, and LocalApi boundary.
3. [API contracts](./api-contracts.md) explains `ApiResponse`, `Result`, and frontend unwrapping.
4. [API versioning](./api-versioning.md) explains `/api/v1`, compatibility rules, OpenAPI CI validation, and migration from unversioned routes.
5. [API error taxonomy](./api-error-taxonomy.md) lists stable error codes, HTTP mappings, and envelope examples.
6. [Document ingestion](./ingestion.md) explains job lifecycle, progress, retry/cancel rules, and diagnostics.
7. [Ingestion and RAG](./rag-pipeline.md) explains upload, semantic search, and chat answers with sources.
8. [AI runtime](./ai-runtime.md) explains provider abstraction, llama.cpp, model listing, chat, and embeddings.
9. [Data and storage](./database.md) explains SQLite, local files, embeddings, and offline persistence.
10. [Frontend architecture](./frontend.md) explains the feature-sliced UI and LocalApi-only communication.
11. [Security and sync](./security-and-sync.md) explains loopback access, token protection, upload guardrails, and offline-first synchronization.
12. [Observability](./observability.md) explains diagnostics and logs.
13. [Diagrams](./diagrams.md) indexes the focused Mermaid diagrams.
14. [Architecture decisions](./decisions/README.md) records why the system is built this way.

## Current Invariants

- The desktop frontend never calls AI runtimes, SQLite, or file storage directly.
- LocalApi is the only desktop HTTP boundary and returns predictable `ApiResponse` envelopes for normal API endpoints.
- Public LocalApi endpoints used by the frontend are exposed under the versioned `/api/v1` prefix.
- Expected application failures use `Result` with stable error codes; unexpected failures are normalized by middleware.
- Documents are ingested through durable lifecycle jobs with progress, retry/cancel affordances, and sanitized diagnostics.
- Runtime integrations are replaceable providers; llama.cpp is the first implemented provider, and Ollama can be added behind the same contracts.
- Generated OpenAPI, Swagger UI assets, and DocFX metadata live under `docs/auto-generated/` and are not authored by hand.
- Breaking changes to an existing versioned public API contract are detected in CI through generated OpenAPI comparison.
