# Repository Hygiene

This repository is configured so source code, architecture documents, configuration templates, scripts, migrations, and CI definitions are tracked, while generated outputs and local runtime data stay out of Git and Docker build contexts.

## Tracked

- Backend source, tests, EF Core migrations, solution files, and central package files.
- Desktop source, Tauri config, lint/typecheck/build config, and `pnpm-lock.yaml`.
- Runtime directory placeholders such as `.gitkeep` and `runtime/ai/README.md`.
- Docker Compose files and SQL initialization scripts.
- Documentation, scripts, Git hooks, and GitHub workflow files.
- Template environment files such as `.env.example`.

## Ignored

- `.env` and any machine-local secret or override file.
- `.NET` outputs: `bin/`, `obj/`, test results, coverage files, local NuGet packages.
- Frontend outputs: `node_modules/`, `dist/`, `.vite/`, caches, logs.
- Tauri/Rust outputs: `target/`.
- Portable/runtime data: SQLite files, uploaded documents, indexes, logs, AI runtime binaries, and model files.
- Release artifacts: `artifacts/`, archives, publish folders.

## Docker Context Rules

The root `.dockerignore` keeps Docker contexts small and prevents local data from leaking into images. Runtime folders, model files, databases, frontend dependencies, build outputs, Git metadata, and local environment files are excluded.

If a future Dockerfile needs documentation or generated UI assets inside the build context, prefer copying those files explicitly in the Dockerfile or add a narrow exception instead of removing broad ignore rules.

## Runtime Data

Portable mode writes to:

```text
runtime/app/data
runtime/app/files
runtime/app/indexes
runtime/app/logs
runtime/ai/bin
runtime/ai/models
```

Only placeholders are tracked. Real user documents, SQLite databases, vector indexes, logs, local AI binaries, and models must never be committed.

## Environment Files

Use `.env.example` as the committed template. Developers can create `.env` locally, but it is ignored. If new required variables are added, update `.env.example` and the setup documentation.
