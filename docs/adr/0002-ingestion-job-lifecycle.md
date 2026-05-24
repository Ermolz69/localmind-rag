# ADR 0002: Ingestion Job Lifecycle

## Status
Accepted

## Context
Document ingestion can fail because files are corrupt, unsupported, empty, or because local runtime dependencies are unavailable. Users and diagnostics need to track, retry, and cancel jobs without reading database internals.

## Decision
Ingestion jobs expose stable API state through list/get/retry/cancel/process endpoints. Jobs track `ProgressPercent`, `CurrentStep`, `ErrorCode`, sanitized `ErrorMessage`, `RetryCount`, operation ids, timestamps, and retry/cancel affordances. Expected lifecycle failures return `Result<T>` with stable ingestion error codes.

The public job lifecycle is:

| Status | Meaning | Progress |
| --- | --- | --- |
| `Pending` | Job is waiting to be claimed. | `0` |
| `Processing` | Job is claimed and file parsing/text extraction is running. | `10-30` |
| `Chunking` | Extracted text is being split into chunks. | `50` |
| `Embedding` | Chunks are being embedded and indexed locally. | `75` |
| `Indexed` | Ingestion completed successfully. | `100` |
| `Failed` | Ingestion failed with a stable error code and sanitized message. | Last known value or `0` |
| `Cancelled` | Job was cancelled before completion. | Last known value or `0` |

Migration maps legacy values as `Queued -> Pending`, `Running -> Processing`, and `Completed -> Indexed`. Legacy `LastError` becomes `ErrorMessage`, and failed jobs with an old error receive `ErrorCode = INGESTION_JOB_FAILED`.

## Consequences
Failed and cancelled jobs can be retried by resetting them to `Pending`, clearing error fields, and incrementing `RetryCount`. Pending and active jobs (`Processing`, `Chunking`, `Embedding`) can be cancelled where safe. Diagnostics can report queue health and latest sanitized failures without exposing raw exception internals.
