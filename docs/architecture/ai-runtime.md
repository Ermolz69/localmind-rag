# AI Runtime

LocalApi owns all AI runtime communication. The frontend never calls Ollama, llama.cpp, or provider sidecars directly.

## Provider Model

Runtime integration is behind provider contracts. Providers expose:

- stable id and display name;
- status and sanitized failure reason;
- capabilities for health, model listing, setup, start/stop, chat, and embeddings;
- configured runtime/model paths and base URL;
- model listing, chat completion, and embedding generation where supported.

llama.cpp is the first implemented provider. The API shape is ready for Ollama or another provider without frontend contract changes.

## LocalApi Endpoints

- `GET /api/runtime/providers` returns provider visibility and selected-provider metadata.
- `GET /api/runtime/status` returns LocalApi readiness, selected provider status, setup requirements, and runtime paths.
- `POST /api/runtime/ai/setup` installs or verifies local runtime assets where supported.
- `POST /api/runtime/ai/start` starts the selected provider where supported.
- `GET /api/runtime/models` lists available models through the selected provider.

RAG chat, semantic search, and ingestion embedding generation depend on provider abstractions instead of concrete runtime clients.
