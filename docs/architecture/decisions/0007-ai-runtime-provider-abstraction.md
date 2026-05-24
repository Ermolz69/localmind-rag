# ADR 0007: AI Runtime Provider Abstraction

## Status
Accepted

## Context
LocalMind needs replaceable AI runtime integration. Development can use Ollama, while portable production targets llama.cpp sidecars and `.gguf` models.

## Decision
Application code depends on `IAiRuntimeProvider` and provider registry abstractions. Providers expose identity, status, capabilities, model listing, chat completion, embedding generation, setup, and start/stop support where available. llama.cpp is the first implemented provider.

## Consequences
Runtime status and model endpoints expose provider-backed DTOs. RAG chat, semantic search, and ingestion use provider abstractions instead of concrete runtime clients. Ollama can be added later without changing frontend contracts.

## Related
- [AI runtime](../ai-runtime.md)
