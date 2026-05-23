# Testing and Coverage

## Standard Checks

Run the full local validation pipeline before pushing:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/check.ps1
```

The compatibility wrapper calls `scripts/check/check.ps1`.

The check script restores and builds the backend, runs backend tests, validates the frontend, builds the frontend, and checks for hardcoded frontend colors.

## Backend Coverage

Run the local coverage workflow when changing backend logic:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/coverage.ps1
```

Linux/macOS:

```bash
./scripts/coverage.sh
```

The compatibility wrappers call `scripts/check/coverage.ps1` and `scripts/check/coverage.sh`.

The coverage script:

- builds the backend solution;
- runs unit and integration tests with `XPlat Code Coverage`;
- runs architecture tests without coverage noise;
- writes test results to `artifacts/test-results/local`;
- writes a merged HTML/Markdown coverage report to `artifacts/coverage-report/local`.

Open the local HTML report:

```text
artifacts/coverage-report/local/index.html
```

Coverage is currently reported as a baseline. The project does not enforce a hard percentage gate yet; new tests should cover meaningful behavior rather than inflate numbers with shallow assertions.

## Test Structure

Backend tests are grouped by test type:

- `KnowledgeApp.UnitTests` covers Application and Infrastructure units.
- `KnowledgeApp.IntegrationTests` covers LocalApi HTTP flows against SQLite.
- `KnowledgeApp.ArchitectureTests` protects project boundaries.

Shared test helpers live under `TestSupport` folders. Prefer these helpers for common setup such as uploaded documents, conversations, embedded chunks, and local test database state.
