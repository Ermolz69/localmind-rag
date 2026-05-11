# localmind-rag

`localmind` is an offline-first desktop knowledge app scaffold for local documents, notes, semantic search, and RAG chat.

## Quick start

```bash
pnpm install
pnpm setup
pnpm dev
```

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

Portable preview:

```bash
pnpm package
```

## Architecture

The desktop UI talks only to `KnowledgeApp.LocalApi`. LocalApi owns SQLite, local file storage, ingestion, vector search, AI runtime adapters, and optional sync workers. Remote sync is isolated in `KnowledgeApp.SyncApi`.

See [docs/architectury/README.md](docs/architectury/README.md) and [docs/architecture-diagrams.md](docs/architecture-diagrams.md).

## GitHub Workflows

- `Check`: full validation on push, pull request, and manual run.
- `Portable Release`: manual or tag-based Windows portable preview artifact build.

## Repository Hygiene

Generated build outputs, runtime data, local databases, AI models, local env files, and release artifacts are ignored. See [docs/repository-hygiene.md](docs/repository-hygiene.md) before adding large files, local runtime assets, or Docker build inputs.
