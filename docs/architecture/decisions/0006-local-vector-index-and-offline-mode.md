# ADR 0006: Local Vector Index And Offline Mode

## Status
Accepted

## Context
LocalMind must work without remote services for document upload, ingestion, semantic search, and chat over local sources. User documents and embeddings should stay on the machine by default.

## Decision
The MVP stores documents, chunks, embeddings, and app state locally. SQLite is the durable database, uploaded files live under `runtime/app/files`, and vector search is hidden behind application interfaces so the implementation can evolve without changing LocalApi contracts.

## Consequences
The app remains usable offline and avoids network dependency for core workflows. Search quality can improve later by replacing the vector search implementation behind the same port. Remote sync remains a separate boundary and does not own local-first behavior.

## Related
- [Data and storage](../database.md)
- [RAG pipeline](../rag-pipeline.md)
