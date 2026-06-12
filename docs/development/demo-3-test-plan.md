# Third demo: test plan, Test Cases, and demonstration script

Preparation date: June 12, 2026.

Product: LocalMind, an offline-first desktop application for local documents, notes, search, and RAG chat.

## 1. Links for the report

- Repository: <https://github.com/Ermolz69/localmind-rag>
- LocalMind bug backlog: <https://github.com/Ermolz69/localmind-rag/issues?q=is%3Aissue%20state%3Aopen%20label%3Abug>
- Current public Kanban board of the owner: <https://github.com/users/Ermolz69/projects/2>
- Bug #82: <https://github.com/Ermolz69/localmind-rag/issues/82>
- Bug #83: <https://github.com/Ermolz69/localmind-rag/issues/83>
- Bug #84: <https://github.com/Ermolz69/localmind-rag/issues/84>

## 2. Testing objective

Verify that the MVP demonstrates the main local-first scenario:

1. the application and LocalApi start without manual database migration;
2. the user creates a bucket;
3. the user uploads a local document;
4. the document is saved, passes validation, and creates an ingestion job;
5. documents are filtered by bucket;
6. the user creates, edits, and deletes a note;
7. errors are shown without internal paths, stack traces, or SQL messages;
8. semantic search and RAG chat work when the AI runtime is available.

## 3. Testing scope

### In scope

- Desktop startup and LocalApi readiness.
- Automatic creation of runtime directories and SQLite.
- Buckets: creation, renaming, deletion, and selection.
- Documents: upload, bucket routing, format validation, list, and filtering.
- Ingestion: states, progress, error feedback, retry/cancel affordances.
- Notes: creation, editing, deletion, and bucket relation.
- Settings, diagnostics, and standard API errors.
- Semantic search and RAG chat when the AI runtime is configured.
- Edge cases for empty values, unsupported files, nullable fields, and unavailable dependencies.

### Out of scope

- Remote sync between devices.
- Registration, login, subscriptions, and cloud backup.
- OCR for scanned PDFs.
- Mobile/web versions.
- Production signing and auto-update.

## 4. Testing approach

- Manual functional testing of key UI flows.
- API smoke testing on an isolated SQLite database.
- Negative testing of upload and validation errors.
- Regression testing through frontend, integration, and architecture tests.
- Exploratory testing around the ingestion lifecycle and frontend/backend contract boundaries.

## 5. Test environment

- OS: Windows.
- Branch: `feature/app-bug-fix`.
- LocalApi URL for the isolated smoke test: `http://127.0.0.1:49431`.
- Database: a separate SQLite database in `artifacts/demo3-smoke`.
- Embedding provider: `Stub`.
- Ingestion worker: disabled for controlled manual job startup.
- Local AI runtime: absent, negative scenario verified.
- Test document: UTF-8 `.txt` with the unique marker `ORION-49321`.
- Unsupported file: `.exe`.

## 6. Entry and exit criteria

### Entry criteria

- Frontend and backend build successfully.
- LocalApi responds to `/api/v1/health`.
- The test SQLite database is isolated from user data.
- `.txt` and unsupported test files are prepared.

### Exit criteria

- All Must scenarios have been executed, or the blocking reason has been recorded.
- Every found defect has reproduction steps, expected/actual result, Severity, and Priority.
- Critical/Major defects have been analyzed before the demo.
- A fallback for the AI runtime has been prepared.

## 7. Result labels

- `PASS` - the actual result matches the expected result.
- `FAIL` - a reproducible defect was found.
- `BLOCKED` - the scenario cannot be completed because of a missing external dependency.
- `NOT RUN` - the scenario is planned for a manual desktop run before the meeting.

## 8. Test Cases

### TC-01. LocalApi startup and health

Related User Stories: US-01, US-04.

Preconditions:

- LocalMind or LocalApi is not running.
- The application port is free.

Steps:

1. Start the desktop application or the isolated LocalApi.
2. Wait for the ready state.
3. Execute `GET /api/v1/health`.

Expected result:

- HTTP status `200`.
- JSON contains `status: "OK"`, `service: "KnowledgeApp.LocalApi"`, and `supervisorInstanceId`.
- The health response is not wrapped in `ApiResponse`.

Result: `PASS`.

### TC-02. First startup creates runtime and SQLite

Related User Stories: US-02, US-03.

Preconditions:

- An empty runtime directory is specified.
- The SQLite file is absent.

Steps:

1. Start LocalApi.
2. Check the runtime directories.
3. Check that the SQLite file was created.
4. Execute a health request.

Expected result:

- `app/data`, `app/files`, `app/indexes`, and `app/logs` are created.
- The database is created and migrations are applied automatically.
- LocalApi switches to the ready state.

Result: `PASS`.

### TC-03. Bucket creation

Related User Story: US-05.

Preconditions:

- LocalApi is ready.
- The Buckets page is open.

Steps:

1. Enter `Demo 3 QA` in the name field.
2. Click **New bucket**.
3. Refresh the list.

Expected result:

- The bucket is created.
- A new card appears in the list.
- The created bucket can be selected.

Result: `PASS`.

### TC-04. Empty bucket name

Type: edge case.

Preconditions:

- The Buckets page is open.

Steps:

1. Leave the field empty or enter only spaces.
2. Click **New bucket**.

Expected result:

- The request does not create a bucket.
- There is no bucket with an empty name in the list.
- The UI remains stable.

Result: `PASS` by frontend guard.

### TC-05. Renaming a bucket without losing other fields

Related User Story: US-08.

Preconditions:

- A bucket named `Demo 3 QA` exists.
- The bucket has the description `Description must survive rename`.

Steps:

1. Open Buckets.
2. Click rename.
3. Change only the name.
4. Save.
5. Fetch the bucket again.

Expected result:

- The name is changed.
- The description is unchanged.

Actual result:

- The frontend sends `description: null`.
- The description is deleted.

Result: `FAIL`, bug #83.

### TC-06. Bucket metadata display

Type: UI edge case.

Preconditions:

- At least one bucket exists.

Steps:

1. Open Buckets.
2. Check the metadata line under the name.

Expected result:

- Sync status has a clear name.
- The separator is displayed as `·`.

Actual result:

- A numeric status is displayed, for example `0`.
- The source contains a corrupted separator `В·`.

Result: `FAIL`, bug #84.

### TC-07. Uploading a supported TXT into the selected bucket

Related User Stories: US-06, US-09.

Preconditions:

- A bucket has been created and selected.
- A `.txt` file is prepared.

Steps:

1. Open Documents.
2. Select the bucket.
3. Upload `.txt` through the file picker or drag-and-drop.
4. Refresh the document list.

Expected result:

- HTTP status `201`.
- The original file is saved locally.
- The document has the selected bucket ID.
- An ingestion job is created.
- The document is present in the list.

Result: `PASS`.

### TC-08. Upload without a selected bucket

Related User Story: US-06.

Preconditions:

- A Default bucket exists.
- **All buckets** is selected in Documents.

Steps:

1. Upload a supported file.
2. Refresh the list.
3. Check the document bucket.

Expected result:

- The backend assigns Default or the defined fallback bucket.
- The document is not left without organization.

Result: `PASS` by integration coverage.

### TC-09. Unsupported file extension

Related User Story: US-10.

Type: negative edge case.

Preconditions:

- The file `unsupported.exe` is prepared.

Steps:

1. Try to upload `.exe`.
2. Check the HTTP response and UI error.

Expected result:

- HTTP status `400`.
- Envelope has `success: false`.
- Error code: `VALIDATION_FAILED`.
- Field detail points to `fileName`.
- There is no stack trace, SQL error, or internal path.

Result: `PASS`.

### TC-10. Filtering documents by bucket

Related User Story: US-07.

Preconditions:

- There are two buckets.
- Each bucket has at least one document.

Steps:

1. Open Documents.
2. Select the first bucket.
3. Check the list.
4. Select the second bucket.
5. Select **All buckets**.

Expected result:

- In bucket view, only documents from the selected bucket are shown.
- All buckets returns the combined list.
- Pagination cursor does not mix results from different filters.

Result: `PASS`.

### TC-11. Full ingestion lifecycle

Related User Stories: US-10, US-12, US-14.

Preconditions:

- A supported document has been uploaded.
- An ingestion job exists.

Steps:

1. Start processing.
2. Observe status, progress, and current step.
3. Cancel the active job if possible.
4. Retry a failed job.
5. Open the status filter.

Expected result:

- The UI supports `Pending`, `Processing`, `Chunking`, `Embedding`, `Indexed`, `Failed`, `Cancelled`.
- Progress/current step is visible.
- Correct retry/cancel actions are available.

Actual result:

- The UI has only `Queued`, `Processing`, `Indexed`, `Failed`.
- Progress/current step, retry, and cancel are not shown fully.

Result: `FAIL`, bug #82.

### TC-12. Missing AI runtime during ingestion

Type: dependency edge case.

Preconditions:

- The AI runtime executable or model is missing.
- The uploaded document is waiting for processing.

Steps:

1. Start ingestion.
2. Wait for completion.
3. Check job details.

Expected result:

- The application does not crash.
- The job moves to `Failed`.
- Error code/message is sanitized.
- The original document is saved.
- Retry is allowed after the runtime is restored.

Result: `PASS` on the backend; UI visibility is partially covered by bug #82.

### TC-13. Creating a note in a bucket

Related User Stories: US-15, US-17.

Preconditions:

- A bucket exists.
- Notes is open.

Steps:

1. Click note creation.
2. Enter title and markdown.
3. Select a bucket.
4. Save.

Expected result:

- The note is created.
- Title, markdown, and bucketId are saved.
- The note appears in the list of the corresponding bucket.

Result: `PASS`.

### TC-14. Editing a note

Related User Story: US-16.

Preconditions:

- A note exists.

Steps:

1. Open the note.
2. Change title and markdown.
3. Click save.
4. Refresh the page.

Expected result:

- Changes are saved.
- Bucket relation is not lost.
- The UI no longer shows the unsaved state.

Result: `PASS`.

### TC-15. Deleting a note

Related User Story: US-16.

Preconditions:

- A note exists.

Steps:

1. Click delete.
2. Confirm the action.
3. Refresh the list.

Expected result:

- The note disappears from the list.
- Reopening does not return the deleted note.
- Other notes are unchanged.

Result: `PASS`.

### TC-16. Cursor pagination

Type: edge case.

Preconditions:

- More records than the page limit have been created.

Steps:

1. Load the first page.
2. Pass `nextCursor`.
3. Check the second page.
4. Pass a malformed cursor.

Expected result:

- Pages do not duplicate elements.
- `hasMore` and `nextCursor` are consistent.
- Malformed cursor returns `VALIDATION_FAILED`.

Result: `PASS` by integration tests.

### TC-17. Settings with missing watchedFolders

Type: nullable contract edge case.

Preconditions:

- The backend response does not contain `watchedFolders` or returns `null`.

Steps:

1. Load Settings.
2. Open the watched folders section.
3. Save settings without manually filling in all fields.

Expected result:

- The frontend mapper substitutes safe defaults.
- The UI does not crash on `.folders`.
- Save sends a valid `AppSettingsDto`.

Result: `PASS`, covered by unit test.

### TC-18. Semantic search

Related User Story: US-19.

Preconditions:

- The document is successfully indexed.
- The embedding provider is ready.

Steps:

1. Open Semantic Search.
2. Enter a query with a fact from the document.
3. Run search.
4. Check sources.

Expected result:

- Relevant chunks are returned.
- Each source contains document, chunk, score, snippet, and page number if available.
- Bucket/document filters narrow the results.

Result: `BLOCKED` in the current manual environment because the AI runtime is missing; backend evaluation/integration coverage passes.

### TC-19. RAG chat with citations

Related User Story: US-20.

Preconditions:

- An indexed document exists.
- Chat provider is ready.

Steps:

1. Create a conversation.
2. Ask a question about the unique fact from the document.
3. Check the answer.
4. Open sources.

Expected result:

- The answer is grounded in the local document.
- Sources correspond to the used chunks.
- Runtime failure is shown as a controlled error.

Result: `BLOCKED` in the current manual environment because the AI runtime is missing; automated RAG evaluation tests are used as fallback evidence.

### TC-20. Portable desktop startup

Related User Stories: US-01, US-22.

Preconditions:

- A portable release artifact is prepared.
- Dev server and LocalApi are not running on the test PC.

Steps:

1. Extract the portable archive into a new directory.
2. Run the desktop executable.
3. Check startup status.
4. Open Documents, Buckets, and Notes.
5. Close the application.
6. Make sure the sidecar process has exited.

Expected result:

- UI and LocalApi start from one executable.
- Docker, IDE, or manual migrations are not required.
- No LocalApi process remains after closing.

Result: `NOT RUN` in this smoke cycle; must be performed on the demo artifact before the meeting.

## 9. Results of completed checks

| Check                                    | Result                             |
| ---------------------------------------- | ---------------------------------- |
| LocalApi health                          | PASS                               |
| Automatic SQLite migrations              | PASS                               |
| Bucket creation                          | PASS                               |
| Upload `.txt`                            | PASS                               |
| Bucket filter                            | PASS                               |
| Unsupported `.exe` validation            | PASS                               |
| Note create/update/delete                | PASS                               |
| Missing AI runtime failure normalization | PASS                               |
| Frontend unit tests                      | 15 PASS                            |
| Backend integration tests                | 113 PASS, 1 container test skipped |
| Backend architecture tests               | 15 PASS                            |
| Frontend lint/typecheck/format/build     | PASS                               |
| Docs/OpenAPI build                       | PASS                               |

## 10. Bug Reports

### BUG-82. Incomplete ingestion lifecycle in Documents

- Issue: <https://github.com/Ermolz69/localmind-rag/issues/82>
- Severity: Major.
- Priority: High.
- Release decision: fix before the MVP release and preferably before the third demo.
- Reason: the defect violates Must User Story US-14.

### BUG-83. Rename bucket deletes description

- Issue: <https://github.com/Ermolz69/localmind-rag/issues/83>
- Severity: Major.
- Priority: Medium.
- Release decision: does not block the main demo flow, but it is silent data loss.
- Reason for the Severity/Priority difference: the impact is serious, but the description field is almost not used in the current UI.

### BUG-84. Numeric sync status and corrupted separator

- Issue: <https://github.com/Ermolz69/localmind-rag/issues/84>
- Severity: Minor.
- Priority: Medium.
- Release decision: fix before the polished demo if there is no risk to core functionality.

## 11. Severity and Priority

Severity describes the technical impact of a defect:

- Critical: the application does not start, main data is lost, or the core workflow is impossible.
- Major: a key function works incorrectly, or there is local data loss.
- Minor: limited UI/UX or cosmetic impact.

Priority describes the fix order:

- High: fix before the demo/release.
- Medium: fix in the nearest sprint, but the defect has a workaround or does not block the core demo.
- Low: can be postponed without risk to the MVP.

Example: bug #83 has Major Severity because of description loss, but Medium Priority because description is not part of the main demo flow.

## 12. Third demo script

Recommended duration: 8-10 minutes.

### Before the meeting

1. Build and verify the portable artifact.
2. Clean or prepare the demo runtime.
3. Create files:
   - `demo-localmind.txt` with a unique fact;
   - one unsupported file for the negative scenario;
   - a small PDF/DOCX if possible.
4. Make sure the AI runtime/model is available.
5. If the AI runtime is unstable, prepare an indexed fixture and screenshot/automated test evidence.
6. Open Kanban, issues #82-#84, and this test plan.
7. Disable unnecessary notifications and close unrelated windows.

### Demonstration sequence

#### 0:00-0:45. Introduction

- Name the product and the problem.
- Say that LocalMind stores documents and notes locally.
- Briefly name the new progress: OpenAPI contracts, buckets, upload/ingestion, notes, search/chat readiness, QA.

#### 0:45-1:30. Startup and runtime

1. Start one desktop executable.
2. Show LocalApi status.
3. Show that manual backend startup is not required.

Comment: runtime folders and SQLite are created automatically.

#### 1:30-2:30. Buckets

1. Open Buckets.
2. Create `Demo 3`.
3. Show bucket selection.

Do not demonstrate bucket rename before bug #83 is fixed.

#### 2:30-4:00. Documents and ingestion

1. Open Documents.
2. Select `Demo 3`.
3. Upload `demo-localmind.txt`.
4. Show that the document appeared in the selected bucket.
5. Start processing.
6. Explain the lifecycle and known bug #82 if it is still open.

#### 4:00-4:45. Negative upload

1. Try to upload an unsupported file.
2. Show a clear validation error.
3. Emphasize that internal paths and stack traces do not leak.

#### 4:45-6:00. Notes

1. Create a note in `Demo 3`.
2. Add title and markdown.
3. Save.
4. Edit one line.
5. Show bucket context.

It is better to show deletion on a separate temporary note.

#### 6:00-7:15. Search or RAG

Main path:

1. Run semantic search for the unique fact.
2. Show the source snippet.
3. Ask the same question in Chat.
4. Show the answer and citations.

Fallback:

- If runtime is unavailable, show diagnostics/error state.
- Show RAG evaluation/integration test result.
- Do not spend time on live debugging.

#### 7:15-8:30. QA and Kanban

1. Open the test plan.
2. Show Test Cases and edge cases.
3. Open issues #82-#84.
4. Explain Severity versus Priority.
5. Name bug #82 as the release blocker and #84 as cosmetic.

#### 8:30-9:00. Closing

- Summarize the end-to-end local-first flow.
- Name known limitations: OCR and remote sync are outside the MVP.
- Move to questions.

## 13. Team roles during the demo

- Presenter: leads the script and works with the UI.
- QA representative: shows the test plan, bugs, and Severity/Priority.
- Technical representative: explains LocalApi, SQLite, OpenAPI contracts, and ingestion.
- Timekeeper/fallback operator: controls time and opens prepared evidence if there is a runtime problem.

If there are fewer participants, roles can be combined, but each participant must present their own part.
