# LocalMind Agent Instructions

LocalMind (`localmind-rag`) is an offline-first desktop knowledge application for local documents, notes, semantic search, and RAG chat. The desktop product remains local-first: user documents, metadata, ingestion jobs, chunks, embeddings, notes, chats, diagnostics, and runtime state are stored and processed on the user's machine by default.

This repository is now a monorepo with the local desktop application, backend sidecar, documentation, infrastructure, and remote/online microservices. When work touches a specific domain, follow the rules for that domain first, then the shared rules in this file.

# Repository Domains

- `apps/desktop` - Tauri 2 + React 19 desktop frontend.
- `backend` - local ASP.NET Core backend sidecar used by the desktop app.
- `services/api-gateway` - .NET API gateway/reverse proxy service with JWT validation.
- `services/auth-service` - NestJS authentication service with JWT/session/device modules and Prisma configuration.
- `services/sync-service` - .NET sync microservice for devices, manifests, sessions, changes, conflicts, queues, and workers.
- `docs` - product, architecture, development, API, and generated documentation.
- `infra` - Docker Compose and remote service infrastructure.
- `runtime` - local runtime data, files, indexes, logs, AI runtime binaries, and AI models.
- `scripts` and `.config/task` - setup, validation, packaging, documentation, coverage, and guard scripts.

# Shared Mandatory Rules

- Derive changes from repository documentation and implementation; do not invent unsupported conventions.
- Preserve existing dirty worktree changes and do not revert user changes unless explicitly asked.
- Use UTF-8, LF line endings, final newline, and spaces according to `.editorconfig`.
- Use 4-space indentation for C#.
- Use 2-space indentation for TypeScript, TSX, JavaScript, JSON, CSS, Markdown, YAML, and YML.
- C# nullable reference types and implicit usings are enabled where the projects already configure them.
- C# warnings are treated as errors in the backend projects; keep code warning-clean.
- C# documentation XML files are generated where configured; keep public XML comments valid when editing documented APIs.
- Package versions for .NET projects are centrally managed where `Directory.Packages.props` exists.
- Frontend TypeScript must pass strict project type checking.
- ESLint forbids `any` through `@typescript-eslint/no-explicit-any`.
- Keep generated build outputs, runtime data, local databases, AI models, local environment files, generated documentation outputs, and release artifacts out of commits.
- Local environment values belong in `.env`, which is ignored.
- Keep `.env.example` updated when introducing required environment variables.
- Prefer existing feature folders, handlers, validators, mappers, ports, API slices, shared UI primitives, and testing helpers over creating parallel patterns.
- Use cancellation tokens where code already follows async/cancellable patterns.
- Add or update meaningful tests when behavior changes.
- Do not inflate coverage with shallow assertions.
- Place code in the existing feature/capability folder structure.
- Update contracts, frontend TypeScript mirrors, OpenAPI metadata, docs, and tests when behavior changes.
- If documentation and implementation disagree, mention the conflict in the work summary and prefer the current implementation unless the documentation explicitly states a requirement that should be restored.
- Do not ban TODO comments unless project documentation is updated to explicitly discourage them.

# Architecture Principles

- The desktop app must call only `KnowledgeApp.LocalApi`, not SQLite, file storage, vector indexes, Ollama, llama.cpp, provider sidecars, or backend internals directly.
- `KnowledgeApp.LocalApi` is the local desktop API boundary. It owns HTTP routes, request parsing, OpenAPI metadata, local security, and API envelope mapping.
- Do not bypass the shared frontend API client for normal JSON API calls.
- Do not bypass `ApiResponse<T>`/`ApiResults` for normal LocalApi endpoints.
- Do not add raw DTO responses for normal LocalApi endpoints.
- Do not introduce ad hoc error codes such as `error`, `failed`, or `bad_request`; use the documented taxonomy or add a stable code deliberately.
- Do not expose raw exception details, internal paths, SQL errors, stack traces, or runtime process output in API errors.
- The remote microservices under `services` are online infrastructure. They must not become required for the desktop app's local-first workflows.
- Keep local-first behavior, local vector indexes, and offline workflows intact unless an ADR explicitly changes that direction.
- Keep feature folders as the canonical navigation model for backend and frontend code.
- Use Clean Architecture-style project boundaries where the service already follows them.
- Application ports isolate use cases from Infrastructure implementations.
- AI runtime integration is provider-based. llama.cpp is currently implemented; Ollama or other providers must fit behind the same contracts.
- Document ingestion is represented as durable lifecycle jobs.
- ADRs are maintained under `docs/architecture/decisions`.

# Microservices

Microservices are independent service domains under `services`. If work touches one of them, inspect that service's README, solution/package files, tests, and current implementation before changing code.

## Service 1: API Gateway

Path: `services/api-gateway`

### Responsibility

- Acts as the remote API gateway/reverse proxy.
- Validates JWTs before proxying requests.
- Uses YARP (`Yarp.ReverseProxy`) for reverse proxy routing.
- Keeps gateway-specific auth/proxy concerns out of downstream services.

### Architecture

- `LocalMind.ApiGateway.Api` owns ASP.NET Core startup, middleware registration, and proxy routing.
- `LocalMind.ApiGateway.Application` owns gateway use cases such as token validation orchestration.
- `LocalMind.ApiGateway.Domain` owns gateway domain models, ports, and exceptions.
- `LocalMind.ApiGateway.Infrastructure` owns concrete implementations such as JWKS/JWT validation and dependency injection.

### Rules

- Keep gateway logic focused on request routing, authentication/authorization boundary checks, and gateway-level policy.
- Do not put downstream service business logic into the gateway.
- Do not bypass the existing token validation port/use case shape when adding auth behavior.
- Keep JWT validation errors sanitized; do not expose raw token contents, secrets, key material, stack traces, or upstream internals.
- Keep YARP route and cluster configuration in service configuration rather than hardcoding unrelated downstream URLs in business code.
- Domain must not depend on Application, Infrastructure, or Api.
- Application must not depend on Infrastructure or Api.
- Infrastructure may implement Domain/Application ports and use external packages.
- Api may compose middleware and dependency injection, but should not contain reusable business logic.

### Validation

```powershell
cd services/api-gateway
dotnet build LocalMind.ApiGateway.slnx
```

## Service 2: Auth Service

Path: `services/auth-service`

### Responsibility

- Provides remote authentication-related functionality.
- Contains NestJS modules for `auth`, `users`, `sessions`, `devices`, `events`, and `prisma`.
- Manages JWT authentication strategy/guards and key loading.
- Uses Prisma configuration for persistence integration.

### Architecture

- `src/main.ts` starts the NestJS application.
- `src/app.module.ts` composes service modules.
- Feature modules live under `src/<domain>`, for example `auth`, `users`, `sessions`, `devices`, `events`, and `prisma`.
- Controllers own HTTP request/response boundaries.
- Services own application logic for their module.
- Prisma access should go through the Prisma module/service.

### Rules

- Keep auth concerns inside the auth service; do not copy authentication business logic into frontend, gateway, or sync service code.
- Keep controllers thin; place logic in services.
- Do not expose private keys, token secrets, password hashes, raw Prisma errors, stack traces, or internal paths in responses.
- Do not commit generated secrets or real certificates. Test/demo keys must be clearly non-production.
- Keep DTOs and request validation explicit when adding endpoints.
- Prefer NestJS dependency injection over manual singleton/global state.
- Keep JWT/session/device logic separated by module unless there is a clear shared abstraction.
- Use Prisma through the existing Prisma service/module rather than direct ad hoc database clients.
- Keep TypeScript strictness and linting expectations intact.

### Validation

```bash
cd services/auth-service
pnpm install
pnpm run lint
pnpm run test
pnpm run test:e2e
pnpm run build
```

Run only the targeted checks needed for small changes, but use the broader set when behavior or contracts change.

## Service 3: Sync Service

Path: `services/sync-service`

### Responsibility

- Independent .NET 10 sync microservice for remote/online sync infrastructure.
- Registers and tracks client devices.
- Creates and inspects sync sessions.
- Accepts push, pull, and manifest requests.
- Publishes sync work to RabbitMQ.
- Persists durable sync state in MongoDB.
- Uses Redis for distributed locks, leases, cursors, and idempotency keys.
- Runs background sync consumers through `LocalMind.Sync.Worker`.

### Architecture

- `LocalMind.Sync.Api` owns HTTP endpoints, endpoint result mapping, request id middleware, and exception normalization.
- `LocalMind.Sync.Contracts` owns public DTOs, queue messages, and API response DTOs.
- `LocalMind.Sync.Application` owns services, mappers, `Result`, application errors, ports, and sync use cases.
- `LocalMind.Sync.Domain` owns sync entities and enums only.
- `LocalMind.Sync.Infrastructure` implements application ports for MongoDB, Redis, RabbitMQ/MassTransit, clocks, queues, and persistence.
- `LocalMind.Sync.Worker` owns background consumers and worker host setup.
- Tests live under `tests/LocalMind.Sync.UnitTests`, `tests/LocalMind.Sync.IntegrationTests`, and `tests/LocalMind.Sync.ArchitectureTests`.

### Rules

- Keep sync service independent from the desktop LocalApi local-first path.
- Do not require sync service availability for local desktop ingestion, search, notes, chats, or runtime workflows.
- Keep Domain free of Application, Infrastructure, Api, Worker, MongoDB, Redis, RabbitMQ, MassTransit, and ASP.NET Core dependencies.
- Keep Application free of Infrastructure, Api, and Worker dependencies.
- Keep Contracts free of Domain dependencies.
- Api endpoints should return through existing endpoint result helpers and standard response DTOs where the service already does so.
- Use existing application ports for devices, sessions, manifests, changes, conflicts, idempotency, distributed locks, clocks, and queue publishing.
- Preserve idempotency and distributed lock behavior for push/pull/diff/conflict operations.
- Queue messages belong in `LocalMind.Sync.Contracts/Queues`.
- Mongo document models and mapping belong in Infrastructure, not Domain or Contracts.
- Redis, MongoDB, RabbitMQ, and MassTransit errors exposed through HTTP must be sanitized.
- Worker consumers should delegate behavior to Application services rather than duplicating business logic.

### Validation

```powershell
cd services/sync-service
dotnet build LocalMind.Sync.slnx
dotnet test LocalMind.Sync.slnx
```

Run architecture tests when changing project references or boundaries.

# Backend

Path: `backend`

The backend is the local ASP.NET Core sidecar for the desktop app. It remains the authoritative local API boundary for frontend-facing local workflows.

## Project Context

- `backend/src/KnowledgeApp.LocalApi` - local HTTP API boundary used by the desktop UI.
- `backend/src/KnowledgeApp.Application` - feature use cases, validation, ports, mappers, `Result` flows.
- `backend/src/KnowledgeApp.Domain` - entities, enums, and value objects.
- `backend/src/KnowledgeApp.Infrastructure` - SQLite, local file storage, ingestion, vector search, embeddings, AI runtime providers, diagnostics, and sync infrastructure.
- `backend/src/KnowledgeApp.Contracts` - public request/response DTOs, API envelope contracts, pagination contracts.
- `backend/src/KnowledgeApp.Bootstrap` - shared startup, security, CORS, middleware, and error handling.
- `backend/src/KnowledgeApp.Observability` - logging and diagnostic helpers.
- `backend/src/KnowledgeApp.SyncApi` - remote sync boundary/skeleton.
- `backend/src/KnowledgeApp.Worker` - worker host.

## Technology

- .NET 10 SDK, pinned by `backend/global.json`.
- ASP.NET Core for LocalApi and SyncApi.
- EF Core 10 with SQLite for local persistence.
- Npgsql package is present for PostgreSQL-related/sync infrastructure.
- FluentValidation.
- Serilog.
- OpenAPI generation through ASP.NET Core OpenAPI tooling.
- DocFX for documentation metadata.
- xUnit.
- Testcontainers for integration tests that need external/containerized services.
- PdfPig and OpenXML-based document extraction packages.
- Central package management through `backend/Directory.Packages.props`.

## Boundaries

- `KnowledgeApp.Domain` contains business entities, enums, and value objects only.
- `KnowledgeApp.Application` contains commands, queries, validators, mappers, `Result` types, application errors, pagination, and application ports.
- `KnowledgeApp.Infrastructure` implements application ports for persistence, file storage, ingestion, vector search, embeddings, runtime providers, diagnostics, sync, OCR, and system services.
- `KnowledgeApp.Contracts` contains public DTOs and shared API contracts used by LocalApi, OpenAPI, DocFX, and frontend mirrors.
- `KnowledgeApp.LocalApi` owns HTTP route mapping, OpenAPI metadata, security middleware, and `ApiResults` conversion.
- Domain must not depend on Application, Infrastructure, LocalApi, SyncApi, Worker, or EF Core.
- Application must not depend on Infrastructure, LocalApi, SyncApi, or Worker.
- Contracts must not depend on Domain.
- LocalApi endpoint modules must not directly use `AppDbContext`, Infrastructure persistence, or Domain entities.
- Keep business logic out of LocalApi endpoint modules.

## API Contracts

- Normal LocalApi endpoints return `ApiResponse<T>` envelopes, except documented exemptions such as health, OpenAPI/static docs, downloads, and streaming endpoints.
- Public frontend-facing LocalApi endpoints use the versioned `/api/v1` prefix.
- Endpoint files define resource-relative routes and must not hardcode the full `/api/v1/...` prefix individually.
- Normal LocalApi endpoint modules should return through `ApiResults` rather than direct `Results.Ok`, `Results.Created`, `Results.Accepted`, `Results.Problem`, `Results.BadRequest`, `Results.NotFound`, `Results.Conflict`, or `Results.NoContent`.
- Non-health LocalApi endpoint metadata must advertise `ApiResponse<T>` response shapes.
- Expected application failures use `Result<T>` or `Result` with stable `ApplicationError` codes.
- Unexpected failures are normalized by middleware and must not expose stack traces, SQL errors, internal paths, raw process output, or raw exception details.
- HTTP status remains authoritative; do not return `200 OK` with `success: false`.
- Validation failures use `VALIDATION_FAILED` and field-level `details`.
- New error codes should come from the existing taxonomy before introducing new stable codes.
- Raw DTOs are not the public LocalApi contract for normal endpoints; clients unwrap `data`.

Normal API success responses use:

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

Normal API error responses use the same envelope with `success: false`, `data: null`, and a stable error code.

Stable error code areas include validation, buckets, documents, notes, chats, ingestion jobs, AI runtime providers, local security, unsupported media, request size, dependency availability, and internal server errors.

## API Versioning

- Frontend-facing public endpoints are under `/api/v1`.
- OpenAPI is exposed at `/openapi/v1.json`.
- Endpoint modules should define resource-relative routes, with the version prefix owned centrally by LocalApi startup.
- Frontend feature modules should use relative API paths; the shared API client owns the `/api/v1` prefix.
- Breaking changes must not be introduced directly into `/api/v1`; create a new API version for breaking contract changes.
- OpenAPI files generated under `docs/auto-generated/openapi` or `artifacts/openapi` are generated artifacts and must not be edited manually.

## Ingestion

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

- LocalApi owns all AI runtime communication for the desktop app.
- Frontend must not call Ollama, llama.cpp, runtime ports, or provider sidecars directly.
- Runtime integration goes through provider contracts such as provider identity, status, capabilities, model listing, chat completion, embedding generation, setup, and start/stop support.
- Runtime failures exposed through the API must be sanitized and mapped to stable envelope errors.
- RAG chat, semantic search, and ingestion embeddings depend on provider abstractions rather than concrete runtime clients.

## Local Security

- LocalApi binds to `127.0.0.1` by default.
- LocalApi is intended for desktop-local access, not public network exposure.
- CORS is restricted to local desktop origins such as `127.0.0.1`, `localhost`, and `tauri.localhost`.
- When configured, mutating endpoints require `X-LocalMind-Token`.
- Uploads validate file name, size, and extension.
- Uploaded file names are sanitized.
- Stored files are written under managed runtime directories.
- LocalApi must not expose arbitrary disk path import behavior through normal upload flows.
- Security and upload failures return standard API envelopes with stable codes.

## Backend Validation

```bash
cd backend
dotnet restore KnowledgeApp.slnx
dotnet build KnowledgeApp.slnx
dotnet test KnowledgeApp.slnx
```

Run RAG evaluation tests when changing retrieval, grounding, embeddings, ranking, or chat context behavior.

# Frontend

Path: `apps/desktop`

## Technology

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

## Architecture

- The desktop frontend is organized with feature-sliced boundaries: `app`, `pages`, `widgets`, `features`, `entities`, and `shared`.
- Frontend code calls only LocalApi through `apps/desktop/src/shared/api`.
- Feature API modules use named API slices such as documents, chats, notes, settings, runtime, diagnostics, buckets, and search.
- The shared API client unwraps `ApiResponse<T>` and throws the standard `ApiError` shape for failed normal responses.
- Runtime providers must never be called directly from frontend code.
- Frontend code must not import backend projects, Domain entities, or generated build output.
- Pages should compose feature public APIs.
- Feature hooks own API orchestration and mutation flows.
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

- Use semantic theme tokens rather than hardcoded colors.
- Frontend hardcoded colors are not allowed in app/page/widget/feature/entity/shared source paths checked by `scripts/check/check-colors.ps1`.
- Reuse existing shared UI primitives.
- Keep frontend components aligned with existing shared UI primitives and semantic theme tokens.
- Keep API orchestration in feature hooks rather than directly in page components.
- Keep DTO mirrors near entities and shared API modules according to existing patterns.

## Frontend Validation

```bash
pnpm --filter desktop lint
pnpm --filter desktop typecheck
pnpm --filter desktop format:check
pnpm --filter desktop build
```

Run the color guard when changing UI styling:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/check/check-colors.ps1
```

# Docs

Path: `docs`

## Documentation Rules

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

## OpenAPI Documentation

- Do not manually edit generated OpenAPI JSON.
- Normal LocalApi endpoints should advertise `ApiResponse<T>` schemas.
- Error responses should use envelope schemas and stable error codes.
- `/api/v1/health` remains a documented exception.
- OpenAPI compatibility checks protect versioned public API contracts in CI.

## Documentation Build

Run after changing docs, endpoint metadata, DTOs, XML comments, OpenAPI behavior, or DocFX configuration:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/docs/build-docs.ps1
```

The script restores DocFX tooling, builds backend projects needed for metadata, generates OpenAPI into `docs/auto-generated/openapi`, generates .NET API metadata into `docs/auto-generated/dotnet-api`, copies Swagger UI assets, and builds the static site into `artifacts/docs/site`.

# Infrastructure And Runtime

- SQLite local database lives under `runtime/app/data`.
- Uploaded files live under `runtime/app/files`.
- Local indexes live under `runtime/app/indexes`.
- Logs live under `runtime/app/logs`.
- llama.cpp runtime assets live under `runtime/ai/bin`.
- AI models live under `runtime/ai/models`.
- Docker Compose files exist for remote sync and microservice infrastructure.
- Do not commit runtime data, SQLite files, uploaded documents, indexes, logs, AI runtime binaries, model files, generated desktop builds, generated docs, or portable release archives.

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

## Test Structure

- `KnowledgeApp.UnitTests` covers Application and Infrastructure units.
- `KnowledgeApp.IntegrationTests` covers LocalApi HTTP flows against isolated SQLite runtime state.
- `KnowledgeApp.RagEvaluationTests` covers business-level RAG/search/chat behavior with controlled fixtures.
- `KnowledgeApp.ArchitectureTests` protects project boundaries and documented architecture rules.
- `LocalMind.Sync.UnitTests` covers sync application and infrastructure units.
- `LocalMind.Sync.IntegrationTests` covers sync infrastructure smoke/integration behavior.
- `LocalMind.Sync.ArchitectureTests` protects sync service project boundaries.
- `services/auth-service/src/**/*.spec.ts` and `services/auth-service/test` cover auth service unit and e2e tests.

Mandatory:

- Use existing `TestSupport` helpers for common setup where available.
- Integration tests use `WebApplicationFactory` in .NET services where existing tests follow that pattern.
- Backend integration tests create isolated SQLite databases under temporary LocalMind runtime directories.
- Tests that need external services use Testcontainers or Docker Compose and require Docker.
- Unit tests and RAG evaluation tests must not require Docker.
- Architecture tests must remain aligned with documented ADR and project boundary rules.

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

Remote infrastructure:

```bash
pnpm docker:up
```

This runs Docker Compose using `infra/docker-compose.remote.yml`.

Portable preview:

```bash
pnpm package
```

GitHub Releases publish portable artifacts through the `Portable Release` workflow. Release notes should describe what changed, what is verified, and what remains intentionally skeleton-level.

## Recommended Agent Workflow

1. Read the relevant docs first:
   - `docs/architecture/README.md`
   - `docs/architecture/backend.md`
   - `docs/architecture/api-contracts.md`
   - `docs/architecture/api-versioning.md`
   - relevant ADRs under `docs/architecture/decisions`
   - relevant development docs under `docs/development`
   - relevant service README and solution/package files under `services` when touching a microservice.
2. Inspect the existing implementation and tests for the feature area.
3. Make the smallest change that follows current project patterns.
4. Update contracts, frontend mirrors, OpenAPI metadata, docs, and tests when behavior changes.
5. Run targeted checks first, then broader checks when appropriate.
6. For documentation-only changes, run architecture tests and docs build when relevant.
7. For backend contract/API changes, run backend tests and docs/OpenAPI generation.
8. For frontend changes, run lint, typecheck, format check, build, and color guard as applicable.
9. For microservice changes, run that service's build/tests and architecture tests if boundaries changed.

## Useful Commands

Use the repository Taskfile as the primary command surface:

```powershell
task -t .config/task/Taskfile.yml --list
```

Setup:

```powershell
task -t .config/task/Taskfile.yml setup
task -t .config/task/Taskfile.yml setup:backend
task -t .config/task/Taskfile.yml setup:frontend
task -t .config/task/Taskfile.yml setup:ai
task -t .config/task/Taskfile.yml setup:ocr
```

Build:

```powershell
task -t .config/task/Taskfile.yml build
task -t .config/task/Taskfile.yml build:backend
task -t .config/task/Taskfile.yml build:frontend
```

Checks:

```powershell
task -t .config/task/Taskfile.yml check
task -t .config/task/Taskfile.yml check:format
task -t .config/task/Taskfile.yml check:backend:format
task -t .config/task/Taskfile.yml check:frontend:format
task -t .config/task/Taskfile.yml check:frontend:lint
task -t .config/task/Taskfile.yml check:frontend:typecheck
task -t .config/task/Taskfile.yml check:colors
task -t .config/task/Taskfile.yml check:docker
```

Tests:

```powershell
task -t .config/task/Taskfile.yml test
task -t .config/task/Taskfile.yml test:unit
task -t .config/task/Taskfile.yml test:integration
task -t .config/task/Taskfile.yml test:rag
task -t .config/task/Taskfile.yml test:architecture
task -t .config/task/Taskfile.yml test:coverage
```

Docs and OpenAPI:

```powershell
task -t .config/task/Taskfile.yml docs:build
task -t .config/task/Taskfile.yml openapi:generate
```

Packaging:

```powershell
task -t .config/task/Taskfile.yml package
task -t .config/task/Taskfile.yml package:smoke-test
```

Microservices:

```powershell
task -t .config/task/Taskfile.yml services:setup
task -t .config/task/Taskfile.yml services:build
task -t .config/task/Taskfile.yml services:test
task -t .config/task/Taskfile.yml services:check
task -t .config/task/Taskfile.yml services:test:coverage
task -t .config/task/Taskfile.yml services:docker:config
task -t .config/task/Taskfile.yml services:docker:up
task -t .config/task/Taskfile.yml services:docker:down
```

Targeted microservice commands:

```powershell
task -t .config/task/Taskfile.yml services:auth:check
task -t .config/task/Taskfile.yml services:auth:start:dev
task -t .config/task/Taskfile.yml services:sync:check
task -t .config/task/Taskfile.yml services:sync:start:api
task -t .config/task/Taskfile.yml services:sync:start:worker
task -t .config/task/Taskfile.yml services:gateway:check
task -t .config/task/Taskfile.yml services:gateway:start
```
