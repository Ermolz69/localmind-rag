# Frontend Contracts

The backend `KnowledgeApp.Contracts` project and generated OpenAPI document are the source of truth for HTTP DTOs, request models, response models, and error envelopes. The desktop frontend currently keeps matching TypeScript types manually.

Frontend contract mirrors live in:

- `apps/desktop/src/entities/*/model/types.ts` for domain-facing API models.
- `apps/desktop/src/shared/api/common.ts` for shared transport models such as cursor pages, `ApiResponse<T>`, and `ApiError`.
- `apps/desktop/src/shared/api/*.ts` for typed API slices.

Rules:

- New backend DTOs must get a matching frontend type before frontend code uses the endpoint.
- Frontend API calls must go through named API slices such as `documentsApi`, `chatsApi`, `notesApi`, or `settingsApi`.
- Frontend code must not import backend projects, Domain entities, or generated build output.
- Pages should not call API slices directly. Feature hooks own API orchestration and pages only compose feature public APIs.
- LocalApi error envelopes should be surfaced through `ApiError` and shared error UI.

Generated OpenAPI is published in the docs site and can be used later to generate frontend types, but manual TypeScript mirrors are still the current implementation.
