# Architecture

LocalMind is a local-first desktop system: a Tauri React UI talks to a loopback-only ASP.NET Core `KnowledgeApp.LocalApi` sidecar, which owns SQLite persistence, local file storage, ingestion, vector search, RAG chat, diagnostics, and AI runtime provider adapters.

This section describes the system as it is implemented now. It avoids future-tense design notes unless a feature is explicitly out of scope.

## Reading Path

1. [System overview](./system.md) explains the runtime shape and project boundaries.
2. [Backend architecture](./backend.md) explains the modular monolith, feature folders, application ports, and LocalApi boundary.
3. [API contracts](./api-contracts.md) explains `ApiResponse<T>`, `Result<T>`, error codes, and frontend unwrapping.
4. [Ingestion and RAG](./rag-pipeline.md) explains upload, ingestion jobs, semantic search, and chat answers with sources.
5. [AI runtime](./ai-runtime.md) explains provider abstraction, llama.cpp, model listing, chat, and embeddings.
6. [Data and storage](./database.md) explains SQLite, local files, embeddings, and offline persistence.
7. [Frontend architecture](./frontend.md) explains the feature-sliced UI and LocalApi-only communication.
8. [Observability](./observability.md) and [local security](./local-security.md) explain diagnostics, logs, loopback access, token protection, and upload guardrails.
9. [Diagrams](./diagrams.md) indexes the focused Mermaid diagrams.
10. [Architecture decisions](./decisions/README.md) records why the system is built this way.

## Current Invariants

- The desktop frontend never calls AI runtimes, SQLite, or file storage directly.
- LocalApi is the only desktop HTTP boundary and returns predictable `ApiResponse<T>` envelopes for normal API endpoints.
- Expected application failures use `Result<T>` with stable error codes; unexpected failures are normalized by middleware.
- Documents are ingested through durable lifecycle jobs with progress, retry/cancel affordances, and sanitized diagnostics.
- Runtime integrations are replaceable providers; llama.cpp is the first implemented provider, and Ollama can be added behind the same contracts.
- Generated OpenAPI, Swagger UI assets, and DocFX metadata live under `docs/auto-generated/` and are not authored by hand.
