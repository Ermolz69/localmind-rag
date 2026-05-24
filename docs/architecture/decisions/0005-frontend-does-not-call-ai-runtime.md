# ADR 0005: Frontend Does Not Call AI Runtime

## Status
Accepted

## Context
The AI runtime may be llama.cpp today and Ollama or another provider later. Direct frontend calls to runtime ports would expose provider-specific URLs, request shapes, errors, and local security assumptions to the UI.

## Decision
The frontend calls only LocalApi. Runtime status, model listing, embeddings, semantic search, and RAG chat are all mediated by backend contracts and provider adapters.

## Consequences
Provider changes do not require frontend transport changes. LocalApi can sanitize runtime failures, enforce local security, and expose stable provider capability metadata. Architecture tests prevent direct frontend references to Ollama, llama.cpp, and provider ports.

## Related
- [AI runtime](../ai-runtime.md)
- [Frontend architecture](../frontend.md)
