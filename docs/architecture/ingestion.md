# Document Ingestion

Document ingestion is a durable lifecycle, not a hidden synchronous upload step. Upload and reindex create jobs; the worker or manual process endpoint claims jobs and advances them through parsing, chunking, embedding, and indexing.

## Lifecycle

| Status | Progress | Current step | Document status | Meaning |
| --- | ---: | --- | --- | --- |
| `Pending` | `0` | `Pending` | `Queued` | Job is waiting to be processed. |
| `Processing` | `10-30` | `Processing` or `Extracting text` | `Processing` | Job is claimed and the source file is being loaded/extracted. |
| `Chunking` | `50` | `Chunking document` | `Processing` | Extracted text is being split into searchable chunks. |
| `Embedding` | `75` | `Generating embeddings` | `Processing` | Chunks are being embedded through the selected runtime provider. |
| `Indexed` | `100` | `Indexed` | `Indexed` | Chunks and embeddings are persisted and available to search. |
| `Failed` | Last known progress | `Failed` | `Failed` | Processing failed with a stable error code and sanitized message. |
| `Cancelled` | Last known progress | `Cancelled` | `Queued` | Job was cancelled before successful indexing. |

## API Fields

`IngestionJobDto` exposes:

| Field | Meaning |
| --- | --- |
| `id` | Job id used by get/process/retry/cancel endpoints. |
| `documentId` | Document owned by the job. |
| `status` | Public lifecycle status. |
| `progressPercent` | Coarse UI/diagnostics progress marker from 0 to 100. |
| `currentStep` | Short step name safe to show in UI. |
| `errorCode` | Stable failure code when status is `Failed`. |
| `errorMessage` | Sanitized user-safe failure message. |
| `retryCount` | Number of explicit retry resets. |
| `canRetry` | True for failed/cancelled jobs. |
| `canCancel` | True for pending or active jobs. |
| `lastOperationId` | Diagnostic operation id for tracing processing logs. |

## Retry And Cancel Rules

- Retry is allowed for `Failed` and `Cancelled`.
- Retry resets the job to `Pending`, clears `errorCode`, `errorMessage`, and `processedAt`, and increments `retryCount`.
- Cancel is allowed for `Pending`, `Processing`, `Chunking`, and `Embedding`.
- Cancel marks the job `Cancelled` and keeps the document in a safe queued state.
- `Indexed` jobs are not cancellable or retryable; reindex creates a new `Pending` job.

## Failure Sanitization

Ingestion failures store stable codes and safe messages:

- known document parsing failures may mention safe terms such as `PDF`, `DOCX`, `PPTX`, `extractable text`, or `Document file`;
- raw exception details, stack traces, SQL errors, local paths, and process output are not exposed;
- generic unexpected ingestion failures use `INGESTION_JOB_FAILED` with a sanitized message.

## Diagnostics

Diagnostics include:

- counts for pending, active, failed, and cancelled jobs;
- latest failed jobs with `errorCode`, `errorMessage`, `retryCount`, `processedAt`, and `lastOperationId`;
- local runtime paths and storage sizes without exposing document content.

The detailed RAG flow is described in [RAG pipeline](./rag-pipeline.md).
