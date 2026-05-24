# ADR 0003: ApiResponse Envelope

## Status
Accepted

## Context
Frontend code needs predictable success and error handling across feature endpoints. Raw DTOs, `ProblemDetails`, and ad hoc anonymous responses make UI error handling inconsistent.

## Decision
Normal LocalApi endpoints return `ApiResponse<T>` with `success`, `data`, `error`, and `metadata`. Error responses use the same envelope with a correct HTTP status code and a stable frontend-facing error code.

## Consequences
The frontend can unwrap responses through one shared `request<T>` helper. New endpoints must advertise `ApiResponse<T>` in OpenAPI metadata. API tests and architecture tests guard against raw DTO and direct `Results.*` regressions.

## Related
- [API contracts](../api-contracts.md)
