# AI Runtime

Development can use Ollama. Portable production targets llama.cpp sidecars and `.gguf` models.

Runtime status is provider-backed. Providers advertise a stable id, display name, status, capabilities, setup guidance, paths, and model listing support. The current implementation registers llama.cpp as the first provider while keeping the API shape ready for additional providers.

LocalApi owns all AI runtime calls. Frontend code never calls Ollama, llama.cpp, or provider sidecars directly; it reads provider visibility through `GET /api/runtime/providers`, runtime health through `GET /api/runtime/status`, and model names through `GET /api/runtime/models`.

Provider implementations must expose the same application-facing operations: health/status, model listing, chat completion, and embedding generation. RAG chat, semantic search, and ingestion depend on the provider abstraction rather than concrete sidecar clients.
