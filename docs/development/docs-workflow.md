# Documentation Workflow

Documentation is authored by hand under `docs/product`, `docs/architecture`, and `docs/development`. Generated documentation is isolated under `docs/auto-generated`.

## Where Things Go

| Content | Location |
| --- | --- |
| Product requirements and priorities | `docs/product/requirements/` |
| Architecture overview, flows, contracts, diagrams, ADRs | `docs/architecture/` |
| Development setup, testing, packaging, docs workflow | `docs/development/` |
| OpenAPI page shell | `docs/api/` |
| Generated OpenAPI, Swagger UI, DocFX metadata | `docs/auto-generated/` |

Do not add new markdown files directly to the docs root unless they are navigation/config entrypoints.


## Generated Files

Do not edit these by hand:

```text
docs/auto-generated/openapi/
docs/auto-generated/dotnet-api/
artifacts/docs/site/
```

The docs build script deletes and recreates generated documentation. Manual edits there will be overwritten.

## Build The Docs

Run:

```bash
task docs:build
```

The script:

- restores DocFX tooling;
- builds backend projects needed for metadata;
- generates OpenAPI JSON into `docs/auto-generated/openapi/openapi.json`;
- generates .NET API metadata into `docs/auto-generated/dotnet-api/`;
- builds the static site into `artifacts/docs/site`.

## Check OpenAPI

After changing endpoints, DTOs, XML comments, or OpenAPI metadata:

1. Run the docs build script.
2. Inspect `docs/auto-generated/openapi/openapi.json`.
3. Confirm representative endpoints advertise `ApiResponse<T>` for normal responses.
4. Confirm error responses use envelope schemas and stable error codes.
5. Confirm `/api/health` remains a documented exception.

## Keep DocFX TOC Healthy

- Add authored architecture pages under the `Architecture` section in `docs/toc.yml`.
- Add development process pages under the `Development` section.
- Keep generated API docs under the existing `API` section.
- Remove TOC entries when deleting or merging pages.
- Prefer one canonical page per topic; avoid keeping old duplicate pages as parallel sources of truth.

## Final Checks

For documentation-only changes, run:

```bash
task test:architecture
task docs:build
```

Run broader backend/frontend checks only when documentation changes also touch code, contracts, scripts, or generated behavior.
