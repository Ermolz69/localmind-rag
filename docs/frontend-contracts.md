# Frontend Contracts

The backend `KnowledgeApp.Contracts` project is the current source of truth for HTTP DTOs, request models, and response models. Until OpenAPI generation is added, the desktop frontend keeps matching TypeScript types manually.

Frontend contract mirrors live in:

- `apps/desktop/src/entities/*/model/types.ts` for domain-facing API models.
- `apps/desktop/src/shared/api/common.ts` for shared transport models such as cursor pages and ProblemDetails.
- `apps/desktop/src/shared/api/*.ts` for typed API slices.

Rules:

- New backend DTOs must get a matching frontend type before frontend code uses the endpoint.
- Frontend API calls must go through named API slices such as `documentsApi`, `chatsApi`, `notesApi`, or `settingsApi`.
- Frontend code must not import backend projects, Domain entities, or generated build output.
- Pages should not call API slices directly. Feature hooks own API orchestration and pages only compose feature public APIs.
- Backend ProblemDetails responses should be surfaced through `ApiError` and shared error UI.

Planned next step: add a lightweight contract smoke test or OpenAPI-based generation once a frontend test runner is introduced.
