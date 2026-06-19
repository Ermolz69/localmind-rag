# Testing and Coverage

## Standard Checks

Three tiers are available:

| Command | What it runs | When to use |
|---|---|---|
| `task check:quick` | Backend format, frontend format/lint/typecheck, API contract drift, color guard | Fast feedback during development |
| `task check` | Everything in `check:quick` plus frontend tests | Default pre-push validation |
| `task check:full` | Everything in `check` plus all backend tests and Rust checks | Before a release or large merge |

`pnpm check` and `pnpm check:quick` at the workspace root delegate to the same tasks.

Run the standard developer check before pushing:

```bash
task -t .config/task/Taskfile.yml check
```

Run only fast static checks (no tests):

```bash
task -t .config/task/Taskfile.yml check:quick
```

Run the full heavy validation suite locally:

```bash
task -t .config/task/Taskfile.yml check:full
```

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

### Unit test helpers (`KnowledgeApp.UnitTests/TestSupport`)

| Helper | Location | Purpose |
|---|---|---|
| `ApplicationTestDatabase` | root | In-memory SQLite database with `AppDbContext`; used in place of private `TestDatabase` copies |
| `FakeOperationLogRepository` | `Fakes/` | No-op `IOperationLogRepository`; replaces per-test private implementations |
| `FakeDomainEventPublisher` | `Fakes/` | Captures `IDomainEvent` in `PublishedEvents`; replaces per-test private implementations |
| `FakeDocumentPreviewConversionService` | `Fakes/` | Always returns `DOCUMENT_PREVIEW_UNSUPPORTED`; used in preview handler tests |
| `StubAppPathProvider` | `Fakes/` | Returns paths from `ManagedFileTestStorage`; implements `IAppPathProvider` |
| `DocumentIngestionTestData` | `Builders/` | Creates a `Document` + `DocumentFile` + `IngestionJob` + temp file in one step; exposes `DocumentId`, `JobId`, `FilePath`; disposes the temp file automatically |
| `DocumentWithFileTestData` | `Builders/` | Creates a `Document` + `DocumentFile` with physical content in managed storage; used in preview handler tests |
| `EmbeddedChunkTestData` | `TestSupport/` | Creates a `Document` + `DocumentChunk` + `DocumentEmbedding` triplet; used in RAG and search tests |
| `ManagedFileTestStorage` | root | Creates an isolated temp directory mirroring the LocalMind managed storage layout; `IAsyncDisposable` |
| `PreviewFixtures` | `Fixtures/` | Text/Markdown/HTML/PDF/unsupported fixture constants and a `BuildPdfBytes` helper |

**Usage example:**

```csharp
await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
await using DocumentIngestionTestData testData = await DocumentIngestionTestData.CreateAsync(
    database, "notes.txt", FileType.PlainText, "content to index");
// testData.DocumentId, testData.JobId, testData.FilePath are available
```

### Integration test helpers (`KnowledgeApp.IntegrationTests/TestSupport`)

| Helper | Purpose |
|---|---|
| `ApiScenarioHelpers.CreateConversationAsync` | POST `/api/v1/chats` and return the created `ConversationDto` |
| `ApiScenarioHelpers.UploadTextDocumentAsync` | Upload a plain-text document via multipart form and return `UploadDocumentResponse` |
| `ApiScenarioHelpers.UploadBytesDocumentAsync` | Upload a binary document via multipart form and return `UploadDocumentResponse` |
| `ApiScenarioHelpers.UploadAndIngestAsync` | Upload and immediately process the pending ingestion job; returns `UploadDocumentResponse` |
| `ApiScenarioHelpers.SendChatMessageAsync` | POST a message to a conversation and return the `RagAnswerDto` |
| `ApiScenarioHelpers.AssertNoLocalPathInResponseBody` | Assert that a response body contains no server-side file paths (path traversal safety) |

### RAG evaluation test helpers (`KnowledgeApp.RagEvaluationTests/TestSupport`)

| Helper | Purpose |
|---|---|
| `ApiScenarioHelpers.CreateConversationAsync` | Create a conversation for an evaluation run |
| `ApiScenarioHelpers.SendChatMessageAsync` | Send a question and return the `RagAnswerDto` |
| `ApiResponseTestExtensions` | `ReadApiDataAsync<T>` extension for asserting on wrapped API responses |

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

## Document Preview Test Strategy

The preview feature splits testing across three layers.

### Handler unit tests (`GetDocumentPreviewHandlerTests`)

Unit tests exercise `GetDocumentPreviewHandler` directly without HTTP.
Each test creates an isolated `ManagedFileTestStorage` (a temp directory) and writes a real file into it via `DocumentWithFileTestData`.
`StubAppPathProvider` points the handler at that temp directory.
`FakeDocumentPreviewConversionService` stubs out the conversion path (DOCX/PPTX) with an unsupported error.

Covered scenarios:

| Test | Asserts |
|---|---|
| Plain text inline | `previewKind = Text`, `textContent` matches file content |
| Markdown inline | `previewKind = Markdown`, `textContent` matches file content |
| HTML inline | `previewKind = Html`, `textContent` matches file content |
| PDF preview URL | `previewKind = Pdf`, `previewUrl` is the expected API path |
| Path outside managed storage | `previewKind = Error`, `errorCode = DOCUMENT_PREVIEW_FILE_MISSING` |
| Inline text exceeds 256 KB | `previewKind = Error`, `errorCode = DOCUMENT_PREVIEW_UNAVAILABLE` |
| Document not found | `result.IsSuccess = false`, `error.Code = DOCUMENT_NOT_FOUND` |

### Preview integration tests (`DocumentPreviewApiTests`)

Integration tests cover the HTTP layer via `WebApplicationFactory`.
They complement the main `DocumentsApiTests.cs` by adding the scenarios that test only inline (text-based) path logic:

| Test | Asserts |
|---|---|
| Markdown file | 200, `previewKind = Markdown`, inline `textContent` |
| HTML file | 200, `previewKind = Html`, inline `textContent` |
| Large text file (>256 KB) | 200, `previewKind = Error`, `errorCode = DOCUMENT_PREVIEW_UNAVAILABLE` |
| Response body safety | No server-local path leaks in the response body |
| Content-type mapping | Correct `contentType` for `.txt`, `.md`, `.html` |

### Frontend hook tests (`useDocumentPreview.test.tsx`)

Hook tests use `renderHook` from `@testing-library/react`.
`@shared/api` is mocked with `vi.mock` so no Tauri runtime or real network calls are needed.

Covered scenarios:

| Test | Asserts |
|---|---|
| Initial state | All fields null/false |
| Loading state | `isLoading = true`, `document` set, `preview = null` while fetching |
| Successful fetch | `preview` populated, `error = null`, `isLoading = false` |
| Failed fetch | `error` populated from `getErrorMessage`, `preview = null` |
| `closePreview` | All fields reset to initial state |
| Race condition | Stale response from first call is discarded when second call wins |

### Frontend test factories

| Factory | File | Returns |
|---|---|---|
| `createTextPreviewResponse` | `test/factories/previewFactories.ts` | `DocumentPreviewResponse` with `previewKind: "Text"` |
| `createMarkdownPreviewResponse` | `test/factories/previewFactories.ts` | `DocumentPreviewResponse` with `previewKind: "Markdown"` |
| `createHtmlPreviewResponse` | `test/factories/previewFactories.ts` | `DocumentPreviewResponse` with `previewKind: "Html"` |
| `createPdfPreviewResponse` | `test/factories/previewFactories.ts` | `DocumentPreviewResponse` with `previewKind: "Pdf"` and a `previewUrl` |
| `createUnsupportedPreviewResponse` | `test/factories/previewFactories.ts` | `DocumentPreviewResponse` with `previewKind: "Unsupported"` |
| `createErrorPreviewResponse` | `test/factories/previewFactories.ts` | `DocumentPreviewResponse` with `previewKind: "Error"` |
| `createDocumentSummary` | `test/factories/documentFactories.ts` | `Schema<"DocumentDto">` with sane defaults |

`renderWithRouter` at `test/helpers/renderWithRouter.tsx` wraps a component in `MemoryRouter` for tests that need a routing context.
