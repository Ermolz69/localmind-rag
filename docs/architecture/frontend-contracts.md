# Frontend Contracts

The backend `KnowledgeApp.Contracts` project and generated OpenAPI document are the source of truth for HTTP DTOs, request models, response models, and error envelopes. The generated TypeScript file is committed so contract changes are visible in reviews.

Frontend contract types live in:

- `apps/desktop/src/shared/contracts/generated.ts` for raw `openapi-typescript` output.
- `apps/desktop/src/shared/contracts/index.ts` for public schema and operation helpers.
- `apps/desktop/src/entities/*/model/types.ts` for compatibility aliases and UI-only models.
- `apps/desktop/src/shared/api/*.ts` for typed API slices.

Rules:

- Manual HTTP DTO mirrors are prohibited. Use `Schema`, `OperationQuery`, `OperationPath`, `OperationJsonBody`, or `OperationData`.
- Entity modules may preserve existing public names through aliases to `@shared/contracts`.
- UI-only state such as drafts, form state, loading state, filter chips, and view models remains local.
- Frontend API calls must go through named API slices such as `documentsApi`, `chatsApi`, `notesApi`, or `settingsApi`.
- Frontend code must not import backend projects or Domain entities.
- Shared API slices must not import entities.
- Pages should not call API slices directly. Feature hooks own API orchestration and pages only compose feature public APIs.
- LocalApi error envelopes should be surfaced through `ApiError` and shared error UI.

Run `task -t .config/task/Taskfile.yml api:generate` after changing backend contracts or endpoint metadata. CI runs `check:api-contracts`, regenerates OpenAPI and TypeScript, and fails when `generated.ts` has drift.
