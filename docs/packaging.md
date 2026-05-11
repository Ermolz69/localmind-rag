# Packaging

Portable output should include `KnowledgeApp.exe`, `KnowledgeApp.LocalApi.exe`, AI runtime binaries, runtime folders, config, and a readme.

Current scaffold packaging creates a portable preview with:

```text
bin/KnowledgeApp.LocalApi.exe
ui/
runtime/app/
runtime/ai/
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
