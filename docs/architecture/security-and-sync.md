# Security and Sync

Security and sync cover the local trust boundary and the offline-first data model. LocalMind treats the desktop machine as the primary runtime and keeps external communication behind explicit API and sync boundaries.

## Topics

- [Local security](./local-security.md) defines loopback binding, local token protection, upload validation, CORS, and sanitized error exposure.
- [Offline/online sync](./offline-online-sync.md) explains the offline-first sync model, local writes, and the outbox shape used for future remote synchronization.

## Invariants

- The desktop frontend communicates only with `KnowledgeApp.LocalApi`.
- LocalApi remains loopback-first and does not expose arbitrary disk access.
- Sync behavior must preserve local-first operation when remote services are unavailable.
