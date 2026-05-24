# Local Runtime

Portable mode keeps LocalMind self-contained. Runtime data and AI assets live beside the packaged app unless configuration points to OS-specific app data folders.

```text
runtime/app/data      SQLite database files
runtime/app/files     uploaded source files
runtime/app/indexes   vector/search indexes
runtime/app/logs      local logs and diagnostic events
runtime/ai/bin        AI runtime binaries
runtime/ai/models     local model files
runtime/ocr           OCR binaries and tessdata
```

## Startup

LocalApi initializes paths, applies SQLite migrations, starts observability, checks local AI runtime status, and reports missing runtime assets through runtime endpoints instead of failing desktop startup.

## Repository Hygiene

Tracked placeholders keep folder shape visible. User documents, databases, indexes, logs, AI binaries, OCR binaries, and model files are ignored and must not be committed.

Related pages:

- [Data and storage](./database.md)
- [AI runtime](./ai-runtime.md)
- [Local security](./local-security.md)
