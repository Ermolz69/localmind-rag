# Project Overview

LocalMind (`localmind-rag`) is an offline-first desktop knowledge application for local documents, notes, semantic search, and RAG chat. It is designed to run as a portable desktop app where a Tauri React UI communicates with a local ASP.NET Core backend sidecar.

The main product goal is local-first personal knowledge management: user documents, metadata, ingestion jobs, chunks, embeddings, notes, chats, diagnostics, and runtime state are stored and processed on the user's machine by default.

# Architecture

## Project Context

The repository is a monorepo with:

- `apps/desktop` - Tauri + React desktop frontend.
- `backend/src/KnowledgeApp.LocalApi` - local HTTP API boundary used by the desktop UI.
- `backend/src/KnowledgeApp.Application` - feature use cases, validation, ports, mappers, `Result` flows.
- `backend/src/KnowledgeApp.Domain` - entities, enums, and value objects.
- `backend/src/KnowledgeApp.Infrastructure` - SQLite, local file storage, ingestion, vector search, embeddings, AI runtime providers, diagnostics, and sync infrastructure.
- `backend/src/KnowledgeApp.Contracts` - public request/response DTOs, API envelope contracts, pagination contracts.
- `backend/src/KnowledgeApp.Bootstrap` - shared startup, security, CORS, middleware, and error handling.
- `backend/src/KnowledgeApp.Observability` - logging and diagnostic helpers.
- `backend/src/KnowledgeApp.SyncApi` - remote sync boundary/skeleton.
- `backend/src/KnowledgeApp.Worker` - worker host.
- `docs` - product, architecture, development, API, and generated documentation.

## Mandatory Rules

- The desktop frontend must call only `KnowledgeApp.LocalApi`, not SQLite, file storage, vector indexes, Ollama, llama.cpp, or provider sidecars directly.
- `KnowledgeApp.LocalApi` is the API boundary. It owns HTTP routes, request parsing, OpenAPI metadata, local security, and API envelope mapping.
- Normal LocalApi endpoints return `ApiResponse<T>` envelopes, except documented exemptions such as health, OpenAPI/static docs, downloads, and streaming endpoints.
- Public frontend-facing LocalApi endpoints use the versioned `/api/v1` prefix.
- Endpoint files define resource-relative routes and must not hardcode the full `/api/v1/...` prefix individually.
- Expected application failures use `Result<T>` or `Result` with stable `ApplicationError` codes.
- Unexpected failures are normalized by middleware and must not expose stack traces, SQL errors, internal paths, raw process output, or raw exception details.
- Domain must not depend on Application, Infrastructure, LocalApi, SyncApi, or EF Core.
- Application must not depend on Infrastructure, LocalApi, SyncApi, or Worker.
- Contracts must not depend on Domain.
- LocalApi endpoint modules must not directly use `AppDbContext`, Infrastructure persistence, or Domain entities.
- LocalApi normal endpoint modules should return through `ApiResults` rather than direct `Results.Ok`, `Results.Created`, `Results.Accepted`, `Results.Problem`, `Results.BadRequest`, `Results.NotFound`, `Results.Conflict`, or `Results.NoContent`.
- Non-health LocalApi endpoint metadata must advertise `ApiResponse<T>` response shapes.

## Architectural Patterns

- Modular monolith with Clean Architecture-style project boundaries.
- Feature folders are the canonical navigation model for backend and frontend code.
- Application ports isolate use cases from Infrastructure implementations.
- LocalApi is a local-first API boundary with loopback-first security.
- AI runtime integration is provider-based. llama.cpp is the first implemented provider; Ollama or other providers should fit behind the same contracts.
- Document ingestion is represented as durable lifecycle jobs.
- ADRs are maintained under `docs/architecture/decisions`.

# Technology Stack

## Backend

- .NET 10 SDK, pinned by `backend/global.json`.
- ASP.NET Core for LocalApi and SyncApi.
- EF Core 10 with SQLite for local persistence.
- Npgsql package is present for PostgreSQL-related/sync infrastructure.
- FluentValidation.
- Serilog for logging.
- OpenAPI generation through ASP.NET Core OpenAPI tooling.
- DocFX for documentation site generation.
- xUnit for tests.
- Testcontainers for .NET in integration tests that need external/containerized services.
- PdfPig and OpenXML-based document extraction packages.
- Central package management through `backend/Directory.Packages.props`.

## Frontend

- Node.js 24.
- pnpm 10.
- Tauri 2.
- React 19.
- TypeScript.
- Vite.
- React Router.
- Tailwind CSS.
- lucide-react.
- Storybook.
- ESLint, Prettier, Stylelint.

## Runtime And Infrastructure

- SQLite local database under `runtime/app/data`.
- Uploaded files under `runtime/app/files`.
- Local indexes under `runtime/app/indexes`.
- Logs under `runtime/app/logs`.
- llama.cpp runtime assets under `runtime/ai/bin`.
- AI models under `runtime/ai/models`.
- Docker Compose files exist for remote sync infrastructure.
- GitHub Actions provide check, docs, OpenAPI compatibility, and portable release workflows.

# Coding Standards

## Mandatory Rules

- Use UTF-8, LF line endings, final newline, and spaces according to `.editorconfig`.
- Use 4-space indentation for C#.
- Use 2-space indentation for TypeScript, TSX, JavaScript, JSON, CSS, Markdown, YAML, and YML.
- C# nullable reference types are enabled.
- C# implicit usings are enabled.
- C# warnings are treated as errors.
- C# documentation XML files are generated.
- Package versions for .NET projects are centrally managed.
- Frontend TypeScript must pass strict project type checking.
- ESLint forbids `any` through `@typescript-eslint/no-explicit-any`.
- Frontend hardcoded colors are not allowed in app/page/widget/feature/entity/shared source paths checked by `scripts/check/check-colors.ps1`; use semantic theme tokens.
- Do not commit generated build outputs, runtime data, local databases, AI models, local environment files, generated documentation outputs, or release artifacts.

## Recommended Practices

- Keep business logic out of LocalApi endpoint modules.
- Prefer existing feature folders, handlers, validators, mappers, ports, and shared API slices over creating parallel patterns.
- Use cancellation tokens where code already follows async/cancellable patterns.
- Keep frontend components aligned with existing shared UI primitives and semantic theme tokens.
- Preserve flexibility where the documentation does not define a strict rule.

# Backend Conventions

## Project Boundaries

Mandatory:

- `KnowledgeApp.Domain` contains business entities, enums, and value objects only.
- `KnowledgeApp.Application` contains commands, queries, validators, mappers, `Result` types, application errors, pagination, and application ports.
- `KnowledgeApp.Infrastructure` implements application ports for persistence, file storage, ingestion, vector search, embeddings, runtime providers, diagnostics, sync, OCR, and system services.
- `KnowledgeApp.Contracts` contains public DTOs and shared API contracts used by LocalApi, OpenAPI, DocFX, and frontend mirrors.
- `KnowledgeApp.LocalApi` owns HTTP route mapping, OpenAPI metadata, security middleware, and `ApiResults` conversion.

Recommended:

- Organize backend code by feature where the project already does so: Buckets, Documents, Ingestion, Search/RAG, Chats, Notes, Runtime, Settings, Diagnostics, Sync, and Health.
- Keep shared code in `Common` or `Abstractions` only when it is genuinely cross-feature.

## API Contracts

Mandatory:

- Normal API success responses use:

```json
{
  "success": true,
  "data": {},
  "error": null,
  "metadata": {
    "timestamp": "2026-05-23T12:30:00Z",
    "requestId": "request-id"
  }
}
```

- Normal API error responses use the same envelope with `success: false`, `data: null`, and a stable error code.
- HTTP status remains authoritative; do not return `200 OK` with `success: false`.
- Validation failures use `VALIDATION_FAILED` and field-level `details`.
- New error codes should come from the existing taxonomy before introducing new stable codes.
- Raw DTOs are not the public LocalApi contract for normal endpoints; clients unwrap `data`.

Stable error code areas include validation, buckets, documents, notes, chats, ingestion jobs, AI runtime providers, local security, unsupported media, request size, dependency availability, and internal server errors.

## API Versioning

Mandatory:

- Frontend-facing public endpoints are under `/api/v1`.
- OpenAPI is exposed at `/openapi/v1.json`.
- Endpoint modules should define resource-relative routes, with the version prefix owned centrally by LocalApi startup.
- Frontend feature modules should use relative API paths; the shared API client owns the `/api/v1` prefix.
- Breaking changes must not be introduced directly into `/api/v1`; create a new API version for breaking contract changes.
- OpenAPI files generated under `docs/auto-generated/openapi` or `artifacts/openapi` are generated artifacts and must not be edited manually.

## Ingestion

Mandatory:

- Document ingestion is a durable job lifecycle, not a hidden synchronous upload operation.
- Public ingestion statuses are `Pending`, `Processing`, `Chunking`, `Embedding`, `Indexed`, `Failed`, and `Cancelled`.
- Ingestion jobs expose progress, current step, error code, sanitized error message, retry count, timestamps, retry/cancel affordances, and diagnostic operation id.
- Upload and reindex create `Pending` jobs.
- The processor claims jobs through `IIngestionJobRepository`.
- Failed ingestion stores stable error codes and sanitized messages.
- Retry is allowed for failed/cancelled jobs and resets the job to pending.
- Cancel is allowed for pending or active jobs where safe.
- Original uploaded files are preserved when ingestion fails.

## AI Runtime

Mandatory:

- LocalApi owns all AI runtime communication.
- Frontend must not call Ollama, llama.cpp, runtime ports, or provider sidecars directly.
- Runtime integration goes through provider contracts such as provider identity, status, capabilities, model listing, chat completion, embedding generation, setup, and start/stop support.
- Runtime failures exposed through the API must be sanitized and mapped to stable envelope errors.
- RAG chat, semantic search, and ingestion embeddings depend on provider abstractions rather than concrete runtime clients.

## Local Security

Mandatory:

- LocalApi binds to `127.0.0.1` by default.
- LocalApi is intended for desktop-local access, not public network exposure.
- CORS is restricted to local desktop origins such as `127.0.0.1`, `localhost`, and `tauri.localhost`.
- When configured, mutating endpoints require `X-LocalMind-Token`.
- Uploads validate file name, size, and extension.
- Uploaded file names are sanitized.
- Stored files are written under managed runtime directories.
- LocalApi must not expose arbitrary disk path import behavior through normal upload flows.
- Security and upload failures return standard API envelopes with stable codes.

# Frontend Conventions

## Architecture

Mandatory:

- The desktop frontend is organized with feature-sliced boundaries: `app`, `pages`, `widgets`, `features`, `entities`, and `shared`.
- Frontend code calls only LocalApi through `apps/desktop/src/shared/api`.
- Feature API modules use named API slices such as documents, chats, notes, settings, runtime, diagnostics, buckets, and search.
- The shared API client unwraps `ApiResponse<T>` and throws the standard `ApiError` shape for failed normal responses.
- Runtime providers must never be called directly from frontend code.
- Frontend code must not import backend projects, Domain entities, or generated build output.
- Pages should compose feature public APIs. Feature hooks own API orchestration and mutation flows.
- Manual TypeScript DTO mirrors must match backend `KnowledgeApp.Contracts` until generated frontend types are introduced.

## Import Boundaries

Mandatory rules are enforced by ESLint:

- Shared layer must not import upper layers.
- Entities must not import features, pages, widgets, or internal shared modules outside public entrypoints.
- Features must use public layer entrypoints and must not import pages/widgets.
- Pages must use feature public APIs and shared public entrypoints.
- Widgets must not import pages/features and must use shared public entrypoints.
- Shared UI primitives must receive state through props and must not import shared model state.
- Do not import the restricted `@shared/api/client`; use named API slices from `@shared/api`.

## UI And Styling

Mandatory:

- Use semantic theme tokens rather than hardcoded colors.
- Frontend must pass ESLint, TypeScript typecheck, Prettier format check, build, and color guard.

Recommended:

- Reuse existing shared UI primitives.
- Keep API orchestration in feature hooks rather than directly in page components.
- Keep DTO mirrors near entities and shared API modules according to existing patterns.

# Testing Requirements

## Mandatory Checks In CI

The `Check` workflow runs:

- Backend format: `cd backend && dotnet format KnowledgeApp.slnx --verify-no-changes --no-restore`.
- Backend build.
- Backend unit tests.
- Backend integration tests.
- Backend RAG evaluation tests.
- Backend architecture tests.
- Frontend Prettier format check.
- Frontend ESLint.
- Frontend TypeScript typecheck.
- Frontend build.
- Frontend hardcoded color guard.
- Docker Compose config validation.

The `OpenAPI compatibility` workflow generates and compares OpenAPI specs and fails on breaking changes to an existing versioned public contract.

The `Docs` workflow builds DocFX/OpenAPI documentation and deploys GitHub Pages from the default branch or manual dispatch.

The `Portable Release` workflow builds and smoke-tests a Windows portable package for manual dispatches or `v*` tags.

## Local Test Commands

Recommended full local validation:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/check.ps1
```

Backend only:

```bash
cd backend
dotnet restore KnowledgeApp.slnx
dotnet build KnowledgeApp.slnx
dotnet test KnowledgeApp.slnx
```

Frontend checks:

```bash
pnpm --filter desktop lint
pnpm --filter desktop typecheck
pnpm --filter desktop format:check
pnpm --filter desktop build
```

Documentation build:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/docs/build-docs.ps1
```

Coverage:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/coverage.ps1
```

Linux/macOS coverage wrapper:

```bash
./scripts/coverage.sh
```

## Test Structure

- `KnowledgeApp.UnitTests` covers Application and Infrastructure units.
- `KnowledgeApp.IntegrationTests` covers LocalApi HTTP flows against isolated SQLite runtime state.
- `KnowledgeApp.RagEvaluationTests` covers business-level RAG/search/chat behavior with controlled fixtures.
- `KnowledgeApp.ArchitectureTests` protects project boundaries and documented architecture rules.

Mandatory:

- Use existing `TestSupport` helpers for common setup where available.
- Integration tests use `WebApplicationFactory`.
- Integration tests create isolated SQLite databases under temporary LocalMind runtime directories.
- Tests that need external services use Testcontainers and require Docker.
- Unit tests and RAG evaluation tests must not require Docker.
- Architecture tests must remain aligned with documented ADR and project boundary rules.

Recommended:

- Add meaningful behavior tests when changing backend logic.
- Do not inflate coverage with shallow assertions.
- Run RAG evaluation tests when changing retrieval, grounding, embeddings, ranking, or chat context behavior.

# Documentation Requirements

## Mandatory Rules

- Hand-authored documentation lives under:
  - `docs/product`
  - `docs/architecture`
  - `docs/development`
  - `docs/api` for API page shells.
- Generated documentation lives under `docs/auto-generated` and must not be edited by hand.
- Generated static site output under `artifacts/docs/site` must not be edited or committed.
- Do not add new Markdown files directly to the docs root unless they are navigation/config entrypoints.
- `docs/architecture/technology-stack-defense.md` is intentionally standalone; do not edit, split, or shorten it unless that file is explicitly in scope.
- Add new architecture pages to the Architecture section in `docs/toc.yml`.
- Add development process pages to the Development section in `docs/toc.yml`.
- Keep generated API docs under the existing API section.
- Remove TOC entries when deleting or merging pages.
- Prefer one canonical page per topic rather than keeping duplicate sources of truth.
- ADRs live under `docs/architecture/decisions`.
- ADRs use the structure: Status, Context, Decision, Consequences, and Related links where useful.

## Documentation Build

Run after changing docs, endpoint metadata, DTOs, XML comments, OpenAPI behavior, or DocFX configuration:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/docs/build-docs.ps1
```

The script restores DocFX tooling, builds backend projects needed for metadata, generates OpenAPI into `docs/auto-generated/openapi`, generates .NET API metadata into `docs/auto-generated/dotnet-api`, copies Swagger UI assets, and builds the static site into `artifacts/docs/site`.

## OpenAPI Documentation

Mandatory:

- Do not manually edit generated OpenAPI JSON.
- Normal LocalApi endpoints should advertise `ApiResponse<T>` schemas.
- Error responses should use envelope schemas and stable error codes.
- `/api/v1/health` remains a documented exception.
- OpenAPI compatibility checks protect versioned public API contracts in CI.

# Workflow

## Setup

Required tools:

- .NET 10 SDK.
- Node.js 24.
- pnpm 10.
- Rust/Cargo for full Tauri packaging.
- Docker for container-backed integration tests and remote sync infrastructure checks.

Initial setup:

```bash
pnpm install
pnpm check
```

Project setup including AI runtime/model setup:

```bash
pnpm setup
```

Development run:

```bash
pnpm dev
```

This runs LocalApi on `http://127.0.0.1:49321` and starts the desktop frontend dev server.

## Runtime And Environment

Mandatory:

- Local environment values belong in `.env`, which is ignored.
- Keep `.env.example` updated when introducing required environment variables.
- Do not commit runtime data, SQLite files, uploaded documents, indexes, logs, AI runtime binaries, model files, generated desktop builds, generated docs, or portable release archives.

Runtime folders:

```text
runtime/app/data
runtime/app/files
runtime/app/indexes
runtime/app/logs
runtime/ai/bin
runtime/ai/models
```

## Packaging And Releases

Portable preview:

```bash
pnpm package
```

GitHub Releases publish portable artifacts through the `Portable Release` workflow. Release notes should describe what changed, what is verified, and what remains intentionally skeleton-level.

# Decision Making

## Mandatory Alignment

New work must align with existing ADRs and architecture documentation:

- Keep the backend as a modular monolith unless a new ADR changes that decision.
- Keep LocalApi as the desktop API boundary.
- Keep normal API responses in the `ApiResponse<T>` envelope.
- Use `Result<T>`/`Result` for expected application failures.
- Keep frontend away from direct AI runtime calls.
- Preserve local vector index/offline-first behavior.
- Add runtime integrations through the provider abstraction.
- Preserve ingestion as a durable job lifecycle.
- Preserve LocalApi local security defaults.

## When Adding Or Changing Features

Mandatory:

- Place code in the existing feature/capability folder structure.
- Keep endpoint URLs and `/api/v1` compatibility rules in mind.
- Avoid breaking existing versioned public contracts; introduce a new API version for breaking changes.
- Update `KnowledgeApp.Contracts` and frontend TypeScript mirrors when changing DTOs used by the frontend.
- Update OpenAPI metadata and docs when API behavior changes.
- Add or update tests at the appropriate level: unit, integration, RAG evaluation, or architecture.
- Add or update ADRs for major architecture decisions.

Recommended:

- Prefer extending existing ports, handlers, mappers, validators, and API slices over creating new parallel abstractions.
- Keep new decisions documented when they affect architecture boundaries, API contracts, runtime providers, storage, sync, or security.
- If documentation conflicts with implementation, prefer the implementation unless the documentation explicitly states a requirement that should be restored.

# Agent Instructions

## Mandatory Rules For AI Agents

- Derive changes from repository documentation and implementation; do not invent unsupported conventions.
- Preserve existing dirty worktree changes and do not revert user changes unless explicitly asked.
- Do not edit generated files under `docs/auto-generated` or generated site output under `artifacts/docs/site` by hand.
- Do not commit runtime data, local databases, AI models, generated documentation, build outputs, local secrets, or release artifacts.
- Do not introduce frontend direct calls to Ollama, llama.cpp, provider sidecars, SQLite, file storage, or backend internals.
- Do not bypass the shared frontend API client for normal JSON API calls.
- Do not bypass `ApiResponse<T>`/`ApiResults` for normal LocalApi endpoints.
- Do not add raw DTO responses for normal LocalApi endpoints.
- Do not introduce ad hoc error codes such as `error`, `failed`, or `bad_request`; use the documented taxonomy or add a stable code deliberately.
- Do not expose raw exception details, internal paths, SQL errors, stack traces, or runtime process output in API errors.
- Do not move business logic into LocalApi endpoint modules.
- Do not add Domain dependencies on outer layers or EF Core.
- Do not add Application dependencies on Infrastructure or API projects.
- Do not add Contracts dependencies on Domain.
- Do not manually change OpenAPI generated artifacts; regenerate them through scripts.
- Do not ban TODO comments unless project documentation is updated to explicitly discourage them.

## Recommended Agent Workflow

1. Read the relevant docs first:
   - `docs/architecture/README.md`
   - `docs/architecture/backend.md`
   - `docs/architecture/api-contracts.md`
   - `docs/architecture/api-versioning.md`
   - relevant ADRs under `docs/architecture/decisions`
   - relevant development docs under `docs/development`
2. Inspect the existing implementation and tests for the feature area.
3. Make the smallest change that follows current project patterns.
4. Update contracts, frontend mirrors, OpenAPI metadata, docs, and tests when behavior changes.
5. Run targeted checks first, then broader checks when appropriate.
6. For documentation-only changes, run architecture tests and docs build.
7. For backend contract/API changes, run backend tests and docs/OpenAPI generation.
8. For frontend changes, run lint, typecheck, format check, build, and color guard as applicable.

## Useful Commands

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/check.ps1
```

```powershell
dotnet test backend/tests/KnowledgeApp.ArchitectureTests/KnowledgeApp.ArchitectureTests.csproj --no-restore
```

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/docs/build-docs.ps1
```

```bash
pnpm --filter desktop lint
pnpm --filter desktop typecheck
pnpm --filter desktop format:check
pnpm --filter desktop build
```

## Conflict Handling

When documentation and implementation disagree, mention the conflict in the work summary and prefer the current implementation unless the documentation explicitly states a requirement that should be restored.
