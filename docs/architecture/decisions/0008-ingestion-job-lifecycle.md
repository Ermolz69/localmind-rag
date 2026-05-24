# ADR 0008: Ingestion Job Lifecycle

## Status
Accepted

## Context
Document parsing, chunking, embedding, and indexing can be long-running and failure-prone. Users and diagnostics need to inspect progress, retry failures, cancel safe jobs, and understand sanitized failure reasons.

## Decision
Document ingestion is represented by durable jobs with public statuses: `Pending`, `Processing`, `Chunking`, `Embedding`, `Indexed`, `Failed`, and `Cancelled`. Jobs expose progress, current step, stable error code, sanitized error message, retry count, timestamps, and diagnostic operation id.

## Consequences
Upload and reindex flows create `Pending` jobs. The processor claims jobs through an application repository port, updates lifecycle steps, marks success as `Indexed`, and stores sanitized failures. Failed and cancelled jobs can be retried; pending and active jobs can be cancelled where safe.

## Related
- [Ingestion and RAG](../rag-pipeline.md)
- [Observability](../observability.md)
