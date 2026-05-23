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
dotnet restore backend/KnowledgeApp.slnx
dotnet build backend/KnowledgeApp.slnx
dotnet test backend/KnowledgeApp.slnx
```

Checks:

```bash
pnpm check
```

Backend coverage:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/coverage.ps1
```

See [docs/testing.md](docs/testing.md).

Script entrypoints are grouped by purpose under `scripts/check`,
`scripts/setup`, `scripts/package`, and `scripts/dev`. Thin root wrappers such
as `scripts/check.ps1` remain for compatibility.

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

See [docs/architectury/README.md](docs/architectury/README.md), [docs/architecture-diagrams.md](docs/architecture-diagrams.md), and [docs/observability.md](docs/observability.md).

## Requirements

The project technical specification, MVP scope, user stories, non-functional requirements, priorities, and risks live in [docs/requirements/technical-specification.md](docs/requirements/technical-specification.md).

## Releases

Project releases are published through GitHub Releases. Release notes describe what changed, what is verified, and what remains intentionally skeleton-level.

## GitHub Workflows

- `Check`: full validation on push, pull request, and manual run.
- `Portable Release`: manual or tag-based Windows portable preview artifact build.

`Check` is split into separate jobs so failures are visible without digging through one long log:

- Backend format
- Backend build
- Backend tests
- Frontend format
- Frontend lint
- Frontend typecheck
- Frontend build
- Frontend color guard
- Docker compose config
- Check summary

## Repository Hygiene

Generated build outputs, runtime data, local databases, AI models, local env files, and release artifacts are ignored. See [docs/repository-hygiene.md](docs/repository-hygiene.md) before adding large files, local runtime assets, or Docker build inputs.
