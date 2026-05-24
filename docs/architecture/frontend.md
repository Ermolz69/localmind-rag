# Frontend Architecture

The desktop frontend is a Tauri React app organized with feature-sliced boundaries.

```mermaid
flowchart TB
    App["app<br/>providers, router, shell"]
    Pages["pages<br/>route composition"]
    Widgets["widgets<br/>larger UI regions"]
    Features["features<br/>user workflows"]
    Entities["entities<br/>domain-facing models"]
    Shared["shared<br/>API client, UI primitives, utilities"]
    LocalApi["KnowledgeApp.LocalApi"]

    App --> Pages
    Pages --> Widgets
    Pages --> Features
    Widgets --> Features
    Features --> Entities
    Features --> Shared
    Entities --> Shared
    Shared --> LocalApi
```

## Rules

- Frontend code calls only LocalApi through `apps/desktop/src/shared/api`.
- Pages compose feature public APIs; feature hooks own API orchestration and mutation flows.
- Runtime providers are never called directly from the frontend.
- API responses are unwrapped by the shared `request<T>` helper, which returns `data` or throws the standard `ApiError`.
- TypeScript API mirrors live near entities and shared API modules until generated frontend types are introduced.

See [Frontend contracts](./frontend-contracts.md) for DTO mirroring rules.
