# ADR 0002: LocalApi As API Boundary

## Status
Accepted

## Context
The desktop UI needs a stable local API for documents, notes, search, chat, settings, diagnostics, and runtime control. Letting the UI call SQLite, local files, vector search, or AI runtime sidecars directly would spread backend concerns into the frontend.

## Decision
`KnowledgeApp.LocalApi` is the only HTTP boundary used by the desktop UI. It owns request parsing, response envelopes, OpenAPI metadata, local security middleware, and conversion from application results into HTTP responses.

## Consequences
Frontend feature code calls LocalApi through the shared API client. Backend internals can change without changing UI transport semantics. Health, OpenAPI, static docs, downloads, and future streaming endpoints are documented exceptions to the normal response envelope rule.

## Related
- [API contracts](../api-contracts.md)
- [LocalApi local security](../local-security.md)
