# Documentation Workflow

Documentation is authored by hand under `docs/product`, `docs/architecture`, and `docs/development`. Generated documentation is isolated under `docs/auto-generated`.

## Where Things Go

| Content                                                 | Location                     |
| ------------------------------------------------------- | ---------------------------- |
| Product requirements and priorities                     | `docs/product/requirements/` |
| Architecture overview, flows, contracts, diagrams, ADRs | `docs/architecture/`         |
| Development setup, testing, packaging, docs workflow    | `docs/development/`          |
| OpenAPI page shell                                      | `docs/api/`                  |
| Generated OpenAPI, Swagger UI, DocFX metadata           | `docs/auto-generated/`       |

Do not add new markdown files directly to the docs root unless they are navigation/config entrypoints.

## Generated Files

Do not edit these by hand:

```text
docs/auto-generated/openapi/
docs/auto-generated/dotnet-api/
docs/auto-generated/dotnet-api-graph.json
artifacts/docs/site/
```

The docs build script deletes and recreates generated documentation. Manual edits there will be overwritten.

## Build The Docs

Run:

```bash
task -t .config/task/Taskfile.yml docs:build
```

The script runs these steps in order:

1. restores DocFX tooling and builds backend projects needed for metadata;
2. **OpenAPI generation** — generates `docs/auto-generated/openapi/openapi.json`;
3. **DocFX metadata generation** — generates `docs/auto-generated/dotnet-api/`;
4. **.NET API graph generation + validation** — generates `docs/auto-generated/dotnet-api-graph.json` from the metadata produced in step 3, then validates it (see below);
5. **DocFX site build** — builds the static site into `artifacts/docs/site`.

Order matters: the graph generator reads the DocFX metadata, so it runs after step 3; the site build runs last so the generated graph JSON is copied into the site. Stale graph output is removed by the build's clean step before regeneration.

The graph data step can also be run on its own (after the DocFX metadata exists) with:

```bash
task -t .config/task/Taskfile.yml docs:graph
```

It generates and then validates the JSON, reads only the `.NET API` metadata, and is independent of the frontend app, OpenAPI endpoints, Storybook, and hand-authored diagrams.

### Graph data validation

`docs/tools/validate-dotnet-api-graph.cjs` checks the generated JSON against `docs/tools/dotnet-api-graph.schema.json` and enforces semantic rules. **It exits non-zero on any error, which fails `docs:build`** — an invalid graph cannot ship silently. It checks that:

- the JSON conforms to the schema (structure, types, enums);
- there is at least one project node and at least one type node (catches an empty graph);
- node ids are unique;
- every edge references existing nodes (catches broken relation edges).

It prints a summary that the docs build surfaces:

```text
Graph summary:
  total nodes : 106
  total edges : 104
  projects    : 2
  namespaces  : 14
  types       : 90
  missing href: 0
```

`missing href` counts documented type nodes without a resolvable DocFX page; it is reported as a warning, not a failure.

The generator and validator have fixture-based unit tests (`docs/tools/generate-dotnet-api-graph.test.cjs`, run with Node's built-in test runner) covering type-kind detection, generic-type href encoding, inherits/implements edges, an empty metadata folder, metadata with missing optional fields, broken relation edges, and schema violations. Run them with:

```bash
task -t .config/task/Taskfile.yml test:docs-graph
```

CI runs these tests and the validation step in `.github/workflows/docs.yml`.

### Generated graph output in the site

`dotnet-api-graph.json` is declared as a DocFX resource, so the site build copies it to `artifacts/docs/site/auto-generated/dotnet-api-graph.json`. The `.NET API Graph` page (`api/dotnet-api-graph.html`) fetches it at `../auto-generated/dotnet-api-graph.json` — i.e. the graph page renders the generated data straight from the built site with no manual copying. The CI docs workflow (`.github/workflows/docs.yml`) calls `docs:build`, so graph generation already runs in CI without an extra step.

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
task -t .config/task/Taskfile.yml test:architecture
task -t .config/task/Taskfile.yml docs:build
```

Run broader backend/frontend checks only when documentation changes also touch code, contracts, scripts, or generated behavior.
