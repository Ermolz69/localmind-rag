# Packaging

Portable output should include `KnowledgeApp.exe`, `KnowledgeApp.LocalApi.exe`, runtime folders, config, and a readme.

Current scaffold packaging creates a portable preview with:

```text
bin/KnowledgeApp.LocalApi.exe
ui/
runtime/app/
runtime/ai/              first-run AI setup target
config/appsettings.json
README.txt
.env.example
```

Build it locally:

```bash
pnpm package
```

The output goes to `artifacts/`, which is ignored by Git. The `Portable Release` GitHub workflow uploads the ZIP as a workflow artifact and attaches it to tag releases matching `v*`.

Runtime folders are intentionally empty in source control except for placeholders. Real SQLite databases, uploaded files, vector indexes, logs, AI runtime binaries, and model files are not tracked.

The portable ZIP does not bundle llama.cpp or GGUF model files by default. On first run, `/api/runtime/status` reports `SetupRequired` when AI assets are missing, and the desktop UI can call `/api/runtime/ai/setup` to download `llama-server.exe` and the default embedding model.
