# API Error Taxonomy

LocalApi error codes are stable frontend-facing contract values. They are part of the public API surface and should not be replaced with ad hoc values such as `error`, `failed`, or `bad_request`.

## Envelope

All normal API failures use the same response shape:

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

HTTP status remains authoritative. Do not return `200 OK` with `success: false`.

## Status Mapping

| Error type | HTTP status | Use when |
| --- | --- | --- |
| `Validation` | `400 Bad Request` | Request body, query, route, file name, size, extension, cursor, or limit is invalid. |
| `Unauthorized` | `401 Unauthorized` | A configured local token is required but missing. |
| `Forbidden` | `403 Forbidden` | Local access or token validation fails. |
| `NotFound` | `404 Not Found` | A requested bucket, document, note, chat, or ingestion job does not exist. |
| `Conflict` | `409 Conflict` | The request conflicts with current state, such as retrying a non-retryable job. |
| `UnsupportedMedia` | `415 Unsupported Media Type` | Content type or media type is not supported at the API boundary. |
| `Unprocessable` | `422 Unprocessable Entity` | JSON is valid but cannot be applied to current business rules. |
| `ExternalDependency` | `503 Service Unavailable` | Local runtime/provider dependency is unavailable. |
| `NotImplemented` | `501 Not Implemented` | Capability is known but not implemented by the selected provider. |
| `Unexpected` | `500 Internal Server Error` | Unhandled failures after middleware sanitization. |

## Stable Codes

| Code | Typical status | Meaning |
| --- | --- | --- |
| `VALIDATION_FAILED` | `400` | Validation failed; use `details[]` for field-level messages. |
| `REQUEST_INVALID` | `400` | Request shape or filter is invalid when no more specific validation code exists. |
| `BUCKET_NOT_FOUND` | `404` | Bucket does not exist or is not available locally. |
| `DOCUMENT_NOT_FOUND` | `404` | Document does not exist or has been deleted. |
| `NOTE_NOT_FOUND` | `404` | Note does not exist or has been deleted. |
| `CHAT_NOT_FOUND` | `404` | Chat/conversation does not exist. |
| `INGESTION_JOB_NOT_FOUND` | `404` | Ingestion job id is unknown. |
| `INGESTION_JOB_NOT_RETRYABLE` | `409` | Job status cannot be retried. |
| `INGESTION_JOB_NOT_CANCELLABLE` | `409` | Job status cannot be cancelled. |
| `INGESTION_JOB_ALREADY_RUNNING` | `409` | Manual process was requested for an active job. |
| `INGESTION_JOB_FAILED` | `400` or `500` depending on boundary | Ingestion failed with a sanitized message. |
| `AI_PROVIDER_NOT_FOUND` | `404` | Configured provider id is unknown. |
| `AI_PROVIDER_UNAVAILABLE` | `503` | Selected provider exists but is unavailable. |
| `AI_PROVIDER_CAPABILITY_UNSUPPORTED` | `501` | Provider does not support the requested capability. |
| `AI_RUNTIME_UNAVAILABLE` | `503` | Runtime dependency cannot serve the request. |
| `AI_MODEL_NOT_FOUND` | `404` | Requested model is not available. |
| `EXTERNAL_DEPENDENCY_UNAVAILABLE` | `503` | External/local dependency is unavailable. |
| `LOCAL_ACCESS_DENIED` | `403` | Request is not from an allowed local address/host. |
| `LOCAL_TOKEN_REQUIRED` | `401` | Mutating request is missing the configured local token. |
| `LOCAL_TOKEN_INVALID` | `403` | Provided local token does not match. |
| `REQUEST_TOO_LARGE` | `413` | Request body exceeds configured LocalApi limit. |
| `UNSUPPORTED_MEDIA_TYPE` | `415` | Request content type is not supported. |
| `INTERNAL_SERVER_ERROR` | `500` | Unexpected server error after sanitization. |

## Usage Rules

- Use `VALIDATION_FAILED` for all field/query/body validation errors and include `details[]`.
- Use `NotFound` errors only when the requested resource identity is valid but absent.
- Use `Conflict` when the resource exists but its current state rejects the operation.
- Use provider/runtime dependency errors for llama.cpp/Ollama/model availability, not generic validation errors.
- Never expose raw exception messages, SQL errors, file paths, stack traces, or runtime process output in `message`.

## Validation Example

```json
{
  "success": false,
  "data": null,
  "error": {
    "code": "VALIDATION_FAILED",
    "message": "Request validation failed.",
    "details": [
      {
        "field": "limit",
        "message": "Limit must be between 1 and 50."
      }
    ]
  },
  "metadata": {
    "timestamp": "2026-05-23T12:30:00Z",
    "requestId": "request-id"
  }
}
```

## Runtime Unavailable Example

```json
{
  "success": false,
  "data": null,
  "error": {
    "code": "AI_RUNTIME_UNAVAILABLE",
    "message": "AI runtime is unavailable.",
    "details": []
  },
  "metadata": {
    "timestamp": "2026-05-23T12:30:00Z",
    "requestId": "request-id"
  }
}
```
