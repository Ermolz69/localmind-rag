# Backend Architecture

The backend follows Clean Architecture:

- Domain has no infrastructure dependencies.
- Application owns use cases and ports.
- Infrastructure implements persistence, runtime, AI, vector, file storage, and sync adapters.
- LocalApi and SyncApi expose HTTP endpoints without business logic.

## Current Hardening Rules

- Expected failures are represented with `Result<T>` or `Result` and stable error codes.
- LocalApi endpoints convert application results through `ApiResults` and return `ApiResponse<T>`.
- Ingestion jobs are managed as a public lifecycle: `Pending`, `Processing`, `Chunking`, `Embedding`, `Indexed`, `Failed`, and `Cancelled`, with progress, current step, stable error code, sanitized error message, retry count, timestamps, and diagnostic operation id.
- Runtime-specific behavior is hidden behind provider contracts; llama.cpp is the first provider.
- LocalApi is local-first: loopback-only by default, with optional token protection for mutating endpoints.

Architecture decisions are recorded under `docs/adr`.

## Feature Structure

Backend code is organized around business features at the API and application-contract boundaries:

- `Buckets`, `Documents`, `Ingestion`, `Search`, `Chats`, `Notes`, `Runtime`, `Settings`, `Diagnostics`, `Sync`, and `Health` are the canonical feature areas.
- `KnowledgeApp.LocalApi/Endpoints/<Feature>` owns HTTP mapping and OpenAPI metadata only.
- `KnowledgeApp.Application/<Feature>` owns commands, queries, validators, mappers, and feature use cases.
- `KnowledgeApp.Contracts/<Feature>` owns public LocalApi request/response DTOs for that feature.
- `KnowledgeApp.Domain` stays focused on entities, enums, and value objects.
- `KnowledgeApp.Infrastructure` is grouped by technical capability: persistence, storage, ingestion, search, embeddings, AI runtime, diagnostics, and sync.

New backend code should start from the feature folder at the boundary and move inward only when shared behavior is genuinely cross-feature.
