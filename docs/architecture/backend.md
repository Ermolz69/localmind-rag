# Backend Architecture

The backend follows Clean Architecture:

- Domain has no infrastructure dependencies.
- Application owns use cases and ports.
- Infrastructure implements persistence, runtime, AI, vector, file storage, and sync adapters.
- LocalApi and SyncApi expose HTTP endpoints without business logic.

## Current Hardening Rules

- Expected failures are represented with `Result<T>` or `Result` and stable error codes.
- LocalApi endpoints convert application results through `ApiResults` and return `ApiResponse<T>`.
- Ingestion jobs are managed as a lifecycle: queued, running, completed, failed, cancelled, retryable, and cancellable.
- Runtime-specific behavior is hidden behind provider contracts; llama.cpp is the first provider.
- LocalApi is local-first: loopback-only by default, with optional token protection for mutating endpoints.

Architecture decisions are recorded under `docs/adr`.
