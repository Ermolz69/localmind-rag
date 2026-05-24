# ADR 0001: LocalApi Envelope And Result

## Status
Accepted

## Context
LocalApi is the desktop HTTP boundary for LocalMind. Frontend code needs predictable success and error handling without coupling to raw DTOs, `ProblemDetails`, or internal exception messages.

## Decision
Normal LocalApi endpoints return `ApiResponse<T>`. Expected application failures use `Result<T>` or `Result` with stable error codes, and the API boundary maps those results to HTTP status codes plus the envelope. Health, OpenAPI, static assets, file downloads, and future streaming endpoints are explicit exceptions.

## Consequences
Frontend clients unwrap `data` and handle `error.code`. New endpoints must use `ApiResults` and advertise `ApiResponse<T>` in OpenAPI metadata.
