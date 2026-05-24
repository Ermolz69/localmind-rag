# ADR 0004: Result For Expected Failures

## Status
Accepted

## Context
Validation failures, not found resources, conflicts, unsupported files, and unavailable local dependencies are normal application outcomes. Throwing exceptions for these flows makes behavior harder to test and risks leaking implementation details through HTTP responses.

## Decision
Expected application failures use `Result<T>` or `Result` with `ApplicationError`, `ErrorType`, stable code, message, and optional field details. LocalApi maps these results to HTTP status codes and `ApiResponse<T>` envelopes.

## Consequences
Handlers remain independent of ASP.NET Core primitives. Exceptions are reserved for infrastructure/runtime failures and unexpected bugs. Validation errors consistently use `VALIDATION_FAILED` with field-level details.

## Related
- [API contracts](../api-contracts.md)
- [Backend architecture](../backend.md)
