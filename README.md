# localmind-rag

`localmind` is an offline-first desktop knowledge app scaffold for local documents, notes, semantic search, and RAG chat.

## Quick start

```bash
pnpm setup
pnpm dev
```

`pnpm setup` installs frontend dependencies, downloads the portable llama.cpp
runtime, and downloads the default local embedding model. Runtime binaries and
models are stored under `runtime/ai/` and are intentionally not committed.

Backend only:

```bash
cd backend
dotnet restore KnowledgeApp.slnx
dotnet build KnowledgeApp.slnx
dotnet test KnowledgeApp.slnx
```

Checks:

```bash
pnpm check
```

Backend coverage:

```bash
task test:coverage
```

See [docs/development/testing.md](docs/development/testing.md).

Script entrypoints are grouped by purpose inside `.config/task/Taskfile.yml`.

Storybook UI primitives:

```bash
pnpm --filter desktop storybook
pnpm --filter desktop build-storybook
```

Portable preview:

```bash
pnpm package
```

## Architecture

The desktop UI talks only to `KnowledgeApp.LocalApi`. LocalApi owns SQLite, local file storage, ingestion, vector search, AI runtime adapters, and optional sync workers. Remote sync is isolated in `KnowledgeApp.SyncApi`.

See [docs/architecture/README.md](docs/architecture/README.md), [docs/architecture/diagrams.md](docs/architecture/diagrams.md), and [docs/architecture/observability.md](docs/architecture/observability.md).

## Requirements

The project technical specification, MVP scope, user stories, non-functional requirements, priorities, and risks live in [docs/product/requirements/technical-specification.md](docs/product/requirements/technical-specification.md).

## Releases

Project releases are published through GitHub Releases. Release notes describe what changed, what is verified, and what remains intentionally skeleton-level.

## GitHub Workflows

- `Check`: full validation on push, pull request, and manual run.
- `Docs`: DocFX and OpenAPI documentation build on pull request, with GitHub Pages deployment from the default branch.
- `Portable Release`: manual or tag-based Windows portable preview artifact build.

`Check` is split into separate jobs so failures are visible without digging through one long log:

- Backend format
- Backend build
- Backend unit tests
- Backend integration tests
- Backend RAG evaluation tests
- Backend architecture tests
- Frontend check: format, lint, typecheck, color guard
- Frontend build
- API contract drift
- Check summary

## Repository Hygiene

Generated build outputs, runtime data, local databases, AI models, local env files, generated documentation, and release artifacts are ignored. See [docs/development/repository-hygiene.md](docs/development/repository-hygiene.md) before adding large files, local runtime assets, or Docker build inputs.


