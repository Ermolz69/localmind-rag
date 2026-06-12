# API Versioning

## Public LocalApi contract

`KnowledgeApp.LocalApi` is the only HTTP boundary used by the desktop frontend.

All public frontend-facing endpoints are exposed under a versioned route prefix:

```text
/api/v1
```

Examples:

```text
GET    /api/v1/health
GET    /api/v1/diagnostics
GET    /api/v1/documents
POST   /api/v1/documents/upload
POST   /api/v1/search/semantic
GET    /api/v1/settings
PUT    /api/v1/settings
GET    /api/v1/runtime/status
```

The generated OpenAPI document for the current public API contract is available at:

```text
/openapi/v1.json
```

The OpenAPI document route is not itself part of the versioned business API surface. It exposes the generated contract for documentation and compatibility validation.

## Backend routing rule

The LocalApi application defines the public route prefix once and maps all public endpoint groups through it.

Current version constants:

```csharp
internal static class ApiVersions
{
    internal const string V1DocumentName = "v1";
    internal const string V1Prefix = "/api/v1";
}
```

Endpoint files define resource-relative routes:

```csharp
app.MapGet("/documents", ...);
app.MapPost("/search/semantic", ...);
app.MapGet("/settings", ...);
```

They must not hardcode the complete `/api/v1/...` prefix individually. The common route group in `Program.cs` owns the version prefix.

## Frontend routing rule

The desktop frontend accesses LocalApi only through the shared API client:

```text
apps/desktop/src/shared/api/http.ts
```

The shared client owns the current API prefix:

```ts
const publicApiPrefix = "/api/v1";
```

Feature API modules use relative paths only:

```ts
request("/documents");
request("/search/semantic");
request("/settings");
```

Feature modules must not independently hardcode `/api/v1`, because changing or adding a future API version must be controlled centrally.

## Backward-compatible changes in `v1`

The following changes may be introduced without creating a new API version:

- adding a new endpoint;
- adding a new optional request property;
- adding a new optional response property;
- documenting an existing response more precisely;
- adding a new optional query parameter;
- adding a new error case without changing existing successful response contracts.

These changes extend the API without invalidating existing frontend requests.

## Breaking changes

The following changes are breaking changes for the public API contract:

- removing an existing endpoint;
- changing an endpoint path;
- changing an HTTP method;
- removing a request or response property;
- renaming a request or response property;
- changing a property type;
- making an optional request property required;
- changing an existing successful response shape incompatibly;
- changing an existing error response contract incompatibly;
- changing a documented status code in a way that breaks existing callers.

A breaking contract change must not be introduced directly into `/api/v1`.

Instead, it requires a new API version, for example:

```text
/api/v2
```

The frontend can then be migrated deliberately to the new contract.

## OpenAPI generation

OpenAPI is generated from `KnowledgeApp.LocalApi` during build and is not edited manually.

The shared generation script is:

```bash
task -t .config/task/Taskfile.yml openapi:generate
```

Documentation generation uses the same script and writes its generated specification under:

```text
docs/auto-generated/openapi/
```

CI generates temporary specifications under:

```text
artifacts/openapi/
```

Generated files in these locations are build artifacts rather than manually maintained API definitions.

## OpenAPI compatibility guard

The workflow:

```text
.github/workflows/openapi.yml
```

protects the public API contract.

For pull requests, it:

1. generates the OpenAPI specification from the base revision;
2. generates the OpenAPI specification from the proposed revision;
3. uploads generated specifications as workflow artifacts;
4. compares both specifications with `oasdiff`;
5. fails when an already versioned public contract receives a breaking change.

The pull request that initially introduces `/api/v1` establishes the first versioned baseline. Since its base branch still exposes unversioned `/api/...` routes, the breaking comparison is skipped for that initial bootstrap migration only.

After `/api/v1` is merged into the base branch, subsequent pull requests are checked against it.

## Migration from unversioned routes

Before API versioning, LocalApi exposed public routes under the unversioned `/api/...` prefix.

The desktop frontend and LocalApi are distributed together, so this migration performs a direct cutover rather than temporarily maintaining duplicate endpoints.

| Previous route          | Versioned route            |
| ----------------------- | -------------------------- |
| `/api/health`           | `/api/v1/health`           |
| `/api/diagnostics`      | `/api/v1/diagnostics`      |
| `/api/runtime/status`   | `/api/v1/runtime/status`   |
| `/api/buckets`          | `/api/v1/buckets`          |
| `/api/documents`        | `/api/v1/documents`        |
| `/api/documents/upload` | `/api/v1/documents/upload` |
| `/api/ingestion/jobs`   | `/api/v1/ingestion/jobs`   |
| `/api/notes`            | `/api/v1/notes`            |
| `/api/chats`            | `/api/v1/chats`            |
| `/api/search/content`   | `/api/v1/search/content`   |
| `/api/search/semantic`  | `/api/v1/search/semantic`  |
| `/api/settings`         | `/api/v1/settings`         |
| `/api/sync/status`      | `/api/v1/sync/status`      |

Legacy unversioned routes are not retained. Requests to old paths such as:

```text
/api/health
/api/documents
/api/settings
```

return `404 Not Found`.

Integration tests verify both the new versioned routes and the absence of legacy unversioned public routes.
