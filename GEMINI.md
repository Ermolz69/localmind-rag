# LocalMind RAG Project Instructions

Welcome to the `localmind-rag` project. This file provides the foundational context, architecture rules, and development workflows for working in this repository.

## Project Overview

**LocalMind** is an offline-first desktop knowledge application for local documents, notes, semantic search, and RAG (Retrieval-Augmented Generation) chat. It is designed to run as a portable desktop app where a Tauri-based React UI communicates with a local ASP.NET Core backend sidecar.

### Tech Stack

- **Backend:** .NET 10 SDK, ASP.NET Core, EF Core 10 (SQLite), FluentValidation, Serilog.
- **Frontend:** Node.js 24, pnpm 10, Tauri 2, React 19, TypeScript, Vite, Tailwind CSS, Storybook.
- **AI Runtime:** llama.cpp (integrated via provider abstractions), local GGUF models.
- **Documentation:** DocFX, OpenAPI (Swagger UI).
- **Task Orchestration:** Taskfile (`task`), pnpm.

## Architecture

The project follows a **Clean Architecture** modular monolith pattern.

### Project Structure

- `apps/desktop`: Tauri + React desktop frontend.
- `backend/src/KnowledgeApp.LocalApi`: Local HTTP API boundary (used by the desktop UI).
- `backend/src/KnowledgeApp.Application`: Feature use cases, validation, ports, and `Result` flows.
- `backend/src/KnowledgeApp.Domain`: Entities, enums, and value objects (no dependencies on outer layers).
- `backend/src/KnowledgeApp.Infrastructure`: SQLite, file storage, vector search, AI runtime adapters, and sync infrastructure.
- `backend/src/KnowledgeApp.Contracts`: Public DTOs and shared API contracts.
- `docs`: Comprehensive documentation (Product, Architecture, Development, API).
- `runtime`: Local storage for data, files, indexes, logs, and AI models (ignored by git).
- `scripts`: Setup, check, and packaging scripts.

### Key Architectural Invariants

- **Desktop UI Isolation:** The frontend must call only `KnowledgeApp.LocalApi`. It never interacts directly with SQLite, file storage, vector indexes, or AI runtimes (llama.cpp, Ollama).
- **API Boundary:** `LocalApi` returns versioned endpoints (`/api/v1`) wrapped in `ApiResponse<T>` envelopes.
- **Failure Handling:** Use `Result<T>` for expected application failures with stable `ApplicationError` codes.
- **Ingestion Lifecycle:** Document ingestion is a durable job lifecycle with states (Pending, Processing, Indexed, etc.), not a synchronous operation.
- **AI Runtime Abstraction:** AI features depend on provider abstractions. llama.cpp is the default; others can be added as providers.

## Development Workflow

### Setup

Ensure you have .NET 10 SDK, Node.js 24, pnpm 10, and Rust/Cargo installed.

```bash
pnpm install
pnpm setup  # Installs dependencies, downloads llama.cpp and default models
```

### Running Locally

```bash
pnpm dev    # Starts LocalApi and the desktop frontend dev server
```

### Building and Testing

- **Full Validation:** `pnpm check` or `task -t .config/task/Taskfile.yml check` (lints, format, color guard, docker config).
- **Backend Tests:** `task -t .config/task/Taskfile.yml test` (Unit, Integration, RAG Evaluation, Architecture).
- **Frontend Build:** `pnpm --filter desktop build`.
- **Docs Build:** `pnpm docs:build` or `task -t .config/task/Taskfile.yml docs:build`.
- **Packaging:** `pnpm package` or `task -t .config/task/Taskfile.yml package` (Windows portable package).

## Development Conventions

- **Indentation:** 4 spaces for C#; 2 spaces for TS/JS, JSON, CSS, MD, YAML.
- **C# Standards:** Nullable reference types enabled, implicit usings enabled, warnings as errors.
- **Frontend Standards:** Strict TypeScript checking, no `any`, no hardcoded colors (use semantic theme tokens).
- **Feature Folders:** Code should be organized by feature (e.g., Buckets, Documents, Ingestion, Search, Chats).
- **Documentation:** Hand-authored docs live in `docs/`. Generated files in `docs/auto-generated/` should NOT be edited manually.

## Agent Instructions (Mandatory)

When operating as an AI agent in this repository, you **MUST** adhere to these rules:

- **Follow Invariants:** Never suggest or implement direct calls from the frontend to AI runtimes, SQLite, or backend internals.
- **API Standards:** Always use `ApiResponse<T>` envelopes for new LocalApi endpoints. Use the existing stable error code taxonomy.
- **Clean Architecture:** Respect project boundaries. Domain must not depend on outer layers. Application must not depend on Infrastructure.
- **Hygiene:** Do not commit runtime data, local databases, AI models, generated documentation, or release artifacts.
- **Verification:** Always run architecture tests (`task -t .config/task/Taskfile.yml test:architecture`) and docs build (`pnpm docs:build`) after significant backend or documentation changes.
- **Types:** Update `KnowledgeApp.Contracts` and frontend TS mirrors when changing API DTOs.
- **Documentation:** Do not edit `docs/auto-generated/` files. Use the provided scripts to regenerate them.

Refer to `AGENTS.md` and `docs/architecture/README.md` for more exhaustive rules and patterns.
