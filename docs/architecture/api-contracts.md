# API Contracts

LocalApi uses a predictable JSON envelope for every normal API endpoint except health checks, static files, file downloads, and documented streaming endpoints (see [Streaming Endpoints](#streaming-endpoints) below).

## Response envelope

Success responses:

```json
{
  "success": true,
  "data": {},
  "error": null,
  "metadata": {
    "timestamp": "2026-05-23T12:30:00Z",
    "requestId": "request-id"
  }
}
```

Error responses:

```json
{
  "success": false,
  "data": null,
  "error": {
    "code": "DOCUMENT_NOT_FOUND",
    "message": "Document was not found.",
    "details": []
  },
  "metadata": {
    "timestamp": "2026-05-23T12:30:00Z",
    "requestId": "request-id"
  }
}
```

The frontend always checks `success` first. When it is true, the payload is in `data`. When it is false, the stable error contract is in `error`.

## HTTP status codes

The JSON envelope does not replace HTTP status codes:

- `200 OK` for successful reads, updates, deletes, and empty command responses.
- `201 Created` for resource creation.
- `202 Accepted` for accepted background/runtime work.
- `400 Bad Request` for request validation failures.
- `404 Not Found` when a requested local resource is missing.
- `409 Conflict` for state conflicts.
- `415 Unsupported Media Type` for unsupported uploads.
- `500 Internal Server Error` for unexpected server failures.
- `503 Service Unavailable` for unavailable external/local runtime dependencies.

Do not return `HTTP 200` with `success: false`.

## Errors

Error `code` values are stable frontend-facing constants. Messages may be refined, but codes should not be changed casually.

Validation failures always use `VALIDATION_FAILED` and field-level `details`.

The full stable code list, HTTP status mapping, usage guidance, and error envelope examples live in [API error taxonomy](./api-error-taxonomy.md). New endpoints should choose an existing stable code from that page before introducing a new one.

## Layering

Services and handlers should not return HTTP primitives. The API boundary converts application results, known application exceptions, and unexpected exceptions into HTTP responses with `ApiResponse<T>`.

Frontend feature code should call the shared `request<T>` client, which unwraps `ApiResponse<T>` and throws a single `ApiError` shape for failed responses.

## Streaming Endpoints

Two endpoints use `text/event-stream` (SSE) and are exempt from the `ApiResponse` envelope:

- `POST /api/v1/chats/{id}/messages/stream` — streams RAG answer chunks as newline-delimited JSON (`data: {…}\n\n`). Sets `Cache-Control: no-cache` and `Connection: keep-alive`. Mid-stream errors are formatted as a final `ApiResponse`-shaped event so the client can distinguish transport errors from application failures.
- `GET /api/v1/runtime/ai/setup/{setupId}/events` — streams runtime setup progress as named SSE events: `progress`, `completed`, `failed`.

Clients must consume these with `EventSource` or a streaming `fetch`, not with the shared `request<T>()` helper.

## Migration notes

Raw DTOs are no longer the public LocalApi contract. Existing clients must unwrap `data` from `ApiResponse<T>` before reading endpoint payloads.

Expected application failures should be represented as `Result<T>` or `Result` with stable `ApplicationError` codes. Exceptions are reserved for infrastructure/runtime failures and truly unexpected cases that middleware normalizes into the same envelope.
