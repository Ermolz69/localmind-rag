# API Contracts

LocalApi uses a predictable JSON envelope for every normal API endpoint except health checks, static files, file downloads, and future streaming endpoints.

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

Current common codes:

- `VALIDATION_FAILED`
- `REQUEST_INVALID`
- `BUCKET_NOT_FOUND`
- `DOCUMENT_NOT_FOUND`
- `NOTE_NOT_FOUND`
- `CHAT_NOT_FOUND`
- `INGESTION_JOB_NOT_FOUND`
- `EXTERNAL_DEPENDENCY_UNAVAILABLE`
- `INTERNAL_SERVER_ERROR`

Validation failures always use `VALIDATION_FAILED` and field-level `details`.

## Layering

Services and handlers should not return HTTP primitives. The API boundary converts application results, known application exceptions, and unexpected exceptions into HTTP responses with `ApiResponse<T>`.

Frontend feature code should call the shared `request<T>` client, which unwraps `ApiResponse<T>` and throws a single `ApiError` shape for failed responses.
