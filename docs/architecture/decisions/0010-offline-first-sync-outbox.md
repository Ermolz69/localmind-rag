# 10. Offline-First Sync Outbox

Date: 2026-06-01

## Status

Accepted

## Context

LocalMind is fundamentally an offline-first desktop knowledge application. While the data processing (LLM, vector search, embeddings, document indexing) happens locally, we anticipate the future need to synchronize user data (buckets, documents, notes, chats) to a remote backend for multi-device sync or remote backup.

We needed a way to robustly track local changes without tightly coupling the core business logic or EF Core transactions to a remote API that may be unavailable or slow. We also must ensure no local updates are lost if the app crashes before sending them.

## Decision

We will implement the **Transactional Outbox Pattern** alongside a `LocalVersion` counter.

1. **`LocalVersion`**: A uniformly applied `long LocalVersion` property is added to the base `Entity` class. Whenever an entity is modified, its version increments.
2. **`SyncOutboxItem`**: An interceptor (`SyncOutboxSaveChangesInterceptor`) hooks into EF Core's `SavingChangesAsync`. For every created, updated, or deleted syncable entity (`Bucket`, `Document`, `Note`, `Conversation`, `ChatMessage`), it serializes the entity into a `SyncOutboxItem` record.
3. **Transactional Guarantees**: Because the outbox record is inserted in the exact same `DbContext` transaction as the entity modification, we guarantee atomicity.
4. **`LocalDevice`**: A local device identity is persisted to stamp outbox operations and avoid circular sync events when applying remote changes in the future.

## Consequences

- **Pros**:
  - Zero performance penalty on the UI layer. Local API requests remain fast.
  - 100% offline reliability. Changes queue up indefinitely until network availability.
  - Future Sync Workers can safely read, process, and retry outbox messages asynchronously.
- **Cons**:
  - The SQLite database size will grow faster due to the outbox table storing JSON snapshots. We will need a pruning mechanism once events are successfully synchronized.
