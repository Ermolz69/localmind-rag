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

`docs/architecture/technology-stack-defense.md` is intentionally kept as a standalone defense document. Do not edit, split, or shorten it unless that file is explicitly in scope.

## Generated Files

Do not edit these by hand:

```text
docs/auto-generated/openapi/
docs/auto-generated/dotnet-api/
docs/auto-generated/swagger-ui/
artifacts/docs/site/
```

The docs build script deletes and recreates generated documentation. Manual edits there will be overwritten.

## Build The Docs

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/docs/build-docs.ps1
```

The script:

- restores DocFX tooling;
- builds backend projects needed for metadata;
- generates OpenAPI JSON into `docs/auto-generated/openapi/openapi.json`;
- generates .NET API metadata into `docs/auto-generated/dotnet-api/`;
- copies Swagger UI assets;
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

```powershell
dotnet test backend/tests/KnowledgeApp.ArchitectureTests/KnowledgeApp.ArchitectureTests.csproj --no-restore
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/docs/build-docs.ps1
```

Run broader backend/frontend checks only when documentation changes also touch code, contracts, scripts, or generated behavior.
