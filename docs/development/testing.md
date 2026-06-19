# Testing and Coverage

## Standard Checks

Run the full local validation pipeline before pushing:

```bash
task -t .config/task/Taskfile.yml check
```

The check task runs static validation: backend format, frontend format, lint, typecheck, API contract drift, Rust format/clippy, and color guard. It does not run builds or tests — use `test:coverage` for backend tests and `test:frontend` for frontend tests.

## Frontend Tests

Run frontend unit and component tests:

```bash
task -t .config/task/Taskfile.yml test:frontend
```

Or directly:

```bash
pnpm --filter desktop test
```

Test files follow these conventions:

- `src/**/*.test.ts` — pure unit tests, run in a Node environment.
- `src/**/*.test.tsx` — React component and hook tests, run in jsdom with `@testing-library/react`.

Component tests have access to `@testing-library/jest-dom` matchers (`toBeInTheDocument`, `toBeVisible`, etc.) via the global setup file at `src/test/setup.ts`.

In CI, the `frontend-tests` job runs after `frontend-check` and uploads no separate artifact — a failure message from Vitest is sufficient for fast feedback.

## Backend Coverage

Run the local coverage workflow when changing backend logic:

```bash
task -t .config/task/Taskfile.yml test:coverage
```

The coverage task:

- builds the backend solution;
- runs unit, integration and RAG evaluation tests with `XPlat Code Coverage`;
- runs architecture tests without coverage noise;
- writes test results to `artifacts/test-results/local`;
- writes a merged HTML/Markdown coverage report to `artifacts/coverage-report/local`.

Open the local HTML report:

```text
artifacts/coverage-report/local/index.html
```

Coverage is currently reported as a baseline.

The project does not enforce a hard percentage gate yet; new tests should cover meaningful behavior rather than inflate numbers with shallow assertions.

## Test Structure

Backend tests are grouped by test type:

- `KnowledgeApp.UnitTests` covers Application and Infrastructure units.
- `KnowledgeApp.IntegrationTests` covers LocalApi HTTP flows against SQLite.
- `KnowledgeApp.RagEvaluationTests` covers business-level RAG/search/chat behavior using controlled local fixtures.
- `KnowledgeApp.ArchitectureTests` protects project boundaries.

Shared test helpers live under `TestSupport` folders.

Prefer these helpers for common setup such as uploaded documents, conversations, embedded chunks, local test database state, and controlled RAG evaluation fixtures.

## RAG Evaluation Tests

RAG evaluation tests are designed to validate retrieval quality and answer grounding rather than endpoint availability.

Run them directly:

```bash
task -t .config/task/Taskfile.yml test:rag
```

The suite uses:

- small local fixture documents;
- an expected questions dataset;
- deterministic fixture embeddings;
- RAG context thresholding;
- explicit no-context assertions.

See [RAG evaluation tests](./rag-evaluation-tests.md) for details.

## Integration Tests With Testcontainers

Integration tests run through `WebApplicationFactory` and create an isolated SQLite database under a temporary LocalMind runtime directory.

Tests that need external services use Testcontainers for .NET and start their containers automatically.

Requirements:

- Docker Desktop or another Docker engine must be running for container-backed integration tests.
- Unit tests and RAG evaluation tests do not require Docker.
- No manually created database is required; LocalApi applies EF migrations during test startup.

Run integration tests locally:

```bash
task -t .config/task/Taskfile.yml test:integration
```

If Docker is unavailable, container-backed tests fail fast.

Check that Docker is running, image pulls are allowed, and no local firewall blocks mapped container ports.

In CI, the integration test job runs on an Ubuntu runner with Docker available. CI should fail when containers cannot start, LocalApi cannot apply migrations, or integration tests fail.
