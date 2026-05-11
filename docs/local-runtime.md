# Local Runtime

Portable mode stores runtime data under `runtime/app` and AI assets under `runtime/ai`. Production installers can switch to OS-specific app data paths.

Tracked placeholders keep the folder structure visible, but generated contents are ignored:

```text
runtime/app/data      SQLite database files
runtime/app/files     uploaded source files
runtime/app/indexes   vector indexes
runtime/app/logs      local logs
runtime/ai/bin        local AI runtime binaries
runtime/ai/models     local model files
```

Do not commit user documents, databases, indexes, logs, AI binaries, or model files.
