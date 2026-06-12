# Observability

`KnowledgeApp.Observability` owns local logging and diagnostic instrumentation for `localmind`.

## Project Boundary

- `KnowledgeApp.Observability` configures Serilog, request logging, and advanced diagnostic events.
- `KnowledgeApp.Bootstrap` calls the observability extension methods but no longer wires Serilog directly.
- Application code depends only on `IAppDiagnosticLogger`, not on Serilog.

## Log Outputs

Logs are written under `runtime/app/logs` by default:

- `localmind.log`: human-readable rolling application log.
- `errors.log`: warnings and errors.
- `advanced-events.ndjson`: structured operation events for important local pipelines.
- `debug-trace.ndjson`: optional Debug/Development trace stream when enabled.

Advanced logs use NDJSON instead of SQLite because they are append-only, easy to inspect, easy to archive, and do not require database migrations.

## Instrumented Flows

The first observability slice emits operation events for:

- document upload;
- ingestion job processing;
- semantic search;
- RAG answer generation;
- AI runtime startup/setup;
- sync status/run skeleton endpoints.

Ingestion diagnostics expose queue health through LocalApi: pending, active (`Processing`, `Chunking`, `Embedding`), failed, and cancelled job counts; latest sanitized failures; retry counts; and the last operation id for failed jobs. Raw exception details, local file paths, and stack traces are not public API data.

Request logging captures method, path, endpoint display name, status code, elapsed time, and trace id. Request and response bodies are disabled by default to avoid leaking user documents or chat content.

## Configuration

`LocalApi` and `SyncApi` use the `Observability` configuration section:

```json
{
  "Observability": {
    "Enabled": true,
    "Mode": "Advanced",
    "LogsPath": "runtime/app/logs",
    "MinimumLevel": "Information",
    "EnableDebugTrace": false,
    "EnableRequestBodyLogging": false,
    "EnableResponseBodyLogging": false,
    "RetainedFileCountLimit": 14
  }
}
```

Portable production keeps error/warning logs and moderate advanced events. Debug trace remains opt-in.

