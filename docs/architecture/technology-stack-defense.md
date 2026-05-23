# Technology Stack Defense

This document explains the technology choices used by the `localmind` MVP and why they fit the project goal: a portable, offline-first desktop application for documents, markdown notes, local indexing, and local AI/RAG workflows.

## Product Goal

`localmind` is designed as a local-first knowledge workspace. The user should be able to download a portable app, launch it, and work with documents and notes without manually starting Docker, PostgreSQL, backend services, migrations, or AI runtime commands.

The MVP prioritizes:

- Offline-first local usage.
- Portable desktop distribution.
- Low runtime overhead.
- Clean separation between UI, local API, domain logic, infrastructure, and persistence.
- Future extensibility for local RAG and optional remote sync.

## Desktop Shell: Tauri

We use Tauri for the desktop shell.

Why Tauri:

- It is significantly lighter than Electron because it does not bundle a full Chromium runtime per application.
- The shell/runtime layer is Rust-based, which gives a small native wrapper, fast startup, and lower memory usage.
- It supports sidecars, which matches our architecture: the desktop shell can start `KnowledgeApp.LocalApi.exe` automatically.
- It is a good fit for portable desktop packaging, where the user launches one executable and the app manages the local backend.

Why not Electron:

- Electron is mature and productive, but it usually consumes more disk space and memory.
- For an offline-first knowledge app that may also run a local API and local AI runtime, keeping the shell lightweight matters.
- Electron would make the portable package heavier without giving us a major MVP advantage.

## Frontend: React + TypeScript

We use React with TypeScript for the desktop UI.

Why React:

- The application is stateful and interactive: documents, buckets, notes, chat, settings, diagnostics, and runtime status all need reactive UI updates.
- React has strong ecosystem support and works well with Vite and Tauri.
- Component composition fits the current UI direction: reusable primitives, feature widgets, page composition, and Storybook-backed components.

Why TypeScript:

- The app talks to typed backend contracts through API slices.
- TypeScript reduces mistakes in DTO usage, cursor pagination, settings forms, and response handling.
- It is especially useful because the frontend mirrors backend contracts until OpenAPI generation is added.

## Frontend Architecture: Feature-Sliced Design

The frontend is organized in a Feature-Sliced Design style:

- `app` for providers, routing, and global app setup.
- `pages` for page composition.
- `widgets` for layout-level UI such as shell/sidebar/topbar.
- `features` for user-facing use cases such as document upload, note editor, settings form, chat message send.
- `entities` for domain model types.
- `shared` for API transport, hooks, theme, and reusable UI primitives.

Why FSD:

- It prevents pages from becoming large files with API calls, local state, and UI markup mixed together.
- It makes feature ownership clearer for a growing app.
- It gives us architectural lint boundaries: shared code cannot import features/pages, widgets stay layout-only, and pages compose public feature APIs.
- It fits the team workflow because separate developers can work on different features without constantly touching the same files.

Why not a simple `components/services/pages` layout:

- That structure is fine for small prototypes, but it becomes unclear once the app has documents, notes, chat, settings, sync, runtime diagnostics, and shared contracts.
- It tends to produce large service files and page components.
- We already have a multi-domain app, so explicit feature boundaries are worth the small upfront structure.

## Backend: ASP.NET Core Local API

We use ASP.NET Core for the local API sidecar.

Why ASP.NET Core:

- The project is backend-heavy: document upload, ingestion, text extraction, chunking, local persistence, diagnostics, settings, future RAG orchestration, and optional sync.
- ASP.NET Core gives a stable HTTP API surface for the frontend.
- Minimal APIs keep endpoint definitions lightweight.
- Hosted services and dependency injection fit future background workers.
- .NET has strong libraries for SQLite, OpenXML, PDF extraction, structured logging, validation, and tests.

Why a local API instead of direct frontend-to-database access:

- The frontend must not read SQLite or local files directly.
- Business logic belongs in backend/application handlers, not React components.
- The same local API can later support Tauri commands, diagnostics, worker coordination, and optional sync.
- This keeps security and data integrity rules centralized.

Why not Node.js backend:

- Node.js would work, but .NET gives us stronger typed domain modeling, EF Core migrations, mature background service patterns, and better fit for the team’s backend architecture goals.
- Local desktop sidecar publishing as a self-contained executable is straightforward in .NET.

## Backend Architecture: Clean Architecture

The backend is split into:

- `Domain`: entities, enums, value objects. No EF Core, HTTP, SQLite, PostgreSQL, Tauri, or AI runtime.
- `Application`: use cases, commands, queries, validators, ports/interfaces.
- `Infrastructure`: EF Core, SQLite, file storage, extractors, vector search, AI adapters, diagnostics implementations.
- `Contracts`: DTOs and request/response models.
- `LocalApi`: HTTP endpoints.
- `Bootstrap`: cross-cutting DI, logging, error handling, middleware.

Why this architecture:

- It keeps core business logic independent from frameworks.
- It lets Infrastructure change without rewriting handlers.
- It supports testing handlers separately from HTTP endpoints.
- It keeps LocalApi thin: endpoints mostly read HTTP parameters and call handlers/services.
- It prepares the codebase for remote sync and more advanced RAG without collapsing into one large API file.

Why not put all logic in controllers/endpoints:

- It would be faster for the first prototype but difficult to maintain.
- Document ingestion, bucket resolution, settings validation, diagnostics, and chat workflows already need reusable application logic.
- Clean boundaries make it easier to add tests and architecture rules.

## Local Database: SQLite

We use SQLite as the local MVP database.

Why SQLite:

- It is embedded and does not require users to install or start a database server.
- It works offline and fits portable mode.
- It is reliable enough for local documents, notes, chats, settings, and ingestion state.
- It integrates well with EF Core migrations.
- A single local database file is easy to create automatically and package around.

Why not local PostgreSQL:

- PostgreSQL is excellent for a server, but it would require installation, a service, Docker, or manual startup.
- That conflicts with the main UX requirement: the user should launch the desktop app and everything should work.
- PostgreSQL remains a good choice for future remote sync backend, not for the local portable runtime.

## ORM: EF Core

We use EF Core for persistence.

Why EF Core:

- It provides migrations for the SQLite schema.
- It maps the domain model to database tables cleanly.
- It supports LINQ queries, async access, and test-friendly database setup.
- It lets us evolve schema with migration PRs.

Why not raw SQL everywhere:

- Raw SQL can be faster for specific queries, but it would add more boilerplate and make schema evolution harder at this stage.
- EF Core is productive and clear for the current MVP tables.
- Performance-critical vector search can still use specialized implementations behind interfaces later.

## Local File Storage

Original uploaded files are stored outside SQLite in the local runtime file storage.

Why:

- Large binary files should not bloat the database.
- Files can be addressed by predictable local paths.
- The database stores metadata: filename, hash, size, type, and local path.
- This separation is useful for future sync, backup, and diagnostics.

## Document Ingestion

Document ingestion is implemented as a local pipeline:

1. Save uploaded file.
2. Create document metadata.
3. Create ingestion job.
4. Extract text.
5. Split text into chunks.
6. Store chunks in SQLite.
7. Mark document as indexed or failed.

Why this approach:

- It gives visible processing status to the user.
- Failed extraction can be stored as a job error instead of crashing the app.
- The pipeline can later be run by a hosted worker automatically.
- It prepares the system for embeddings and local RAG.

## Vector Search And AI Runtime

The MVP keeps vector search and AI runtime behind application abstractions.

Current direction:

- Store embeddings locally.
- Run semantic search locally.
- Use local AI runtime adapters such as Ollama for development and llama.cpp for portable distribution.

Why abstractions:

- AI runtimes and embedding models may change.
- Embeddings depend on the model and should not be tightly coupled to HTTP endpoints.
- A simple exact vector search can be replaced with SQLite vector extensions or a sidecar index without rewriting the RAG pipeline.

## Remote API Is Out Of Scope For MVP

The repository contains `KnowledgeApp.SyncApi`, but full remote sync is not the focus of the current MVP.

Why:

- The assignment and current milestone focus on proving the local desktop data flow.
- Offline-first functionality must work without any remote dependency.
- Remote sync requires auth, devices, manifests, conflict resolution, and file transfer semantics. These are planned but not required for the MVP walking skeleton.

The architecture still leaves a clear future path:

- Local sync operations are queued in `sync_outbox`.
- Remote sync can later use PostgreSQL and remote file storage.
- Embeddings should remain local by default because they depend on the local model.

## CI And Packaging

The repository has workflows for checks and portable release builds.

Why:

- CI proves the code compiles, tests pass, frontend checks pass, and formatting/linting rules are enforced.
- Portable release builds prove the desktop app can be packaged as a user-facing artifact.
- This supports the project requirement that code is integrated into the shared Git repository.

Relevant files:

- `.github/workflows/check.yml`
- `.github/workflows/portable-release.yml`
- `scripts/check.ps1`
- `scripts/package.ps1`

