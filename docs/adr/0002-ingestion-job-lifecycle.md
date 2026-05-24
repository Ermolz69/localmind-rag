# ADR 0002: Ingestion Job Lifecycle

## Status
Accepted

## Context
Document ingestion can fail because files are corrupt, unsupported, empty, or because local runtime dependencies are unavailable. Users and diagnostics need to track, retry, and cancel jobs without reading database internals.

## Decision
Ingestion jobs expose stable API state through list/get/retry/cancel/process endpoints. Jobs track attempts, sanitized last errors, operation ids, timestamps, and retry/cancel affordances. Expected lifecycle failures return `Result<T>` with stable ingestion error codes.

## Consequences
Failed and cancelled jobs can be retried. Queued and running jobs can be cancelled. Diagnostics can report queue health and latest sanitized failures.
