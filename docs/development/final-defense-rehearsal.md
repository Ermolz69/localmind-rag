# Final defense rehearsal plan

This page is the source text for the team Google document that must be submitted before the final defense rehearsal.

## Submission links

- Google document: `TODO: paste the shared Google Doc link here after the team creates it`
- Release Kanban board: <https://github.com/users/Ermolz69/projects/2>
- Repository: <https://github.com/Ermolz69/localmind-rag>
- Release Candidate branch: `feature/app-bug-fix`

## Code freeze rule

The team applies a code freeze for the Release Candidate.

Allowed changes:

- Critical and major bug fixes that protect the demo scenario.
- Small UI text fixes, documentation updates, and release preparation tasks.
- Test, build, packaging, and environment fixes needed to stabilize the Release Candidate.

Not allowed during code freeze:

- New features.
- Large refactors.
- New architecture directions.
- Risky UI redesigns.
- Scope expansion beyond the final defense demo.

Cut decision:

If a feature is incomplete or unstable, the team will remove it from the live demo narrative and keep the stable local-first MVP flow as the priority.

## Presentation narrative

Recommended structure for the final defense:

1. Problem and context.
2. Live demo.
3. Architecture and technical decisions.
4. Team conclusions.

### 1. Problem and context

Speaker: `@Ermol`.

Message:

LocalMind solves the problem of scattered local knowledge. Students, researchers, and knowledge workers keep information in PDFs, DOCX files, text files, presentations, and notes. Searching manually is slow, while cloud AI tools can be unavailable, costly, or unsuitable for private documents. LocalMind keeps documents, metadata, indexing, notes, search, and RAG chat on the user's machine by default.

Key points:

- Offline-first desktop knowledge workspace.
- Local document ingestion and SQLite persistence.
- Local semantic search and RAG chat when the AI runtime is available.
- Clear API boundary through `KnowledgeApp.LocalApi`.

### 2. Live demo

Demo driver: `@FeadenGlow`.

Narrator: `@Ermol`.

Demo flow:

1. Launch LocalMind and show LocalApi readiness.
2. Open Buckets and create or select the demo bucket.
3. Upload a supported `.txt` document.
4. Show the ingestion lifecycle: `Pending`, `Processing`, `Chunking`, `Embedding`, `Indexed`, `Failed`, or `Cancelled` depending on the environment.
5. Show retry/cancel affordances if the job fails or remains active.
6. Open Documents and filter by bucket/status.
7. Create and edit a note connected to the local workspace.
8. Run semantic search or RAG chat if the local AI runtime is ready.
9. If AI runtime is missing, show the safe degraded state and explain that local data remains available.

Demo rule:

The demo must use the Release Candidate build or the exact branch selected for the final defense. Do not demonstrate unfinished features that are not part of the stable MVP scenario.

### 3. Architecture and technical decisions

Speakers: `@Ermol` and `@uzun-dmytro`.

Key points:

- Tauri desktop app owns desktop lifecycle, sidecar startup, local paths, and process supervision.
- React frontend owns UI state and calls only the shared LocalApi client.
- `KnowledgeApp.LocalApi` owns frontend-facing HTTP routes, response envelopes, OpenAPI metadata, and local security.
- Application layer owns use cases, validation, `Result` flows, and ports.
- Infrastructure implements SQLite persistence, ingestion, file storage, vector search, embeddings, diagnostics, and runtime providers.
- Ingestion is a durable job lifecycle, not a hidden synchronous upload.
- API responses use `ApiResponse<T>` envelopes and stable error codes.
- Local-first behavior is preserved. Remote services are not required for the desktop MVP.

### 4. Team conclusions

Speaker: `@nikkkitosss`.

Message:

The team prioritized a stable local-first MVP over unfinished scope. The final Release Candidate demonstrates the core workflow: local startup, bucket organization, document upload, ingestion visibility, notes, search, and safe handling of unavailable dependencies. Remaining work can continue after the defense as follow-up release tasks.

## Role distribution

| Role | Responsibility | Team member |
| --- | --- | --- |
| Lead narrator | Opens the presentation, explains problem/context, keeps timing | `@Ermol` |
| Demo driver | Clicks through the live demo and prepares the environment | `@FeadenGlow` |
| Architecture speaker | Explains backend/frontend/runtime decisions | `@Ermol`, `@uzun-dmytro` |
| QA/release speaker | Explains code freeze, bug backlog, risks, and Plan B | `@Ermol` |
| Team conclusions speaker | Summarizes team decisions and remaining release work | `@nikkkitosss` |
| Backup driver | Takes over if the demo machine or app fails | `TODO` |

## Plan B

### Risk: LocalApi or desktop app fails to start

Action plan:

1. Restart the application once.
2. If it still fails, show prepared screenshots or a recorded demo of the Release Candidate flow.
3. Explain the Tauri supervisor and LocalApi readiness model.
4. Continue with architecture and QA sections.

### Risk: Local AI runtime is missing or fails

Action plan:

1. Treat it as an expected degraded mode.
2. Show local documents, buckets, notes, diagnostics, and ingestion states.
3. Explain that semantic search and RAG depend on the provider abstraction and can run when the configured local runtime is available.
4. Avoid claiming that AI-dependent flows were demonstrated live if they were not.

### Risk: SQLite database is missing or corrupted

Action plan:

1. Switch to a clean prepared runtime directory.
2. Restart the Release Candidate.
3. Re-run the minimal demo flow with a prepared `.txt` file.
4. If recovery takes too long, use screenshots or recorded demo evidence and explain local runtime isolation.

### Risk: Internet connection is unavailable

Action plan:

1. Continue the demo without internet because the MVP is local-first.
2. Use already opened documentation or local screenshots for GitHub/Kanban references.
3. Explain that remote sync and online services are not required for the desktop MVP flow.

### Risk: Live demo data is wrong or missing

Action plan:

1. Use prepared sample files.
2. Use a prepared demo bucket name.
3. Keep a clean runtime backup.
4. Do not improvise new flows outside the script.

## Rehearsal checklist

Before the meeting:

- Confirm that every team member can attend.
- Confirm the Release Candidate branch/build.
- Stop new feature work and keep only bug/release tasks in the Kanban board.
- Prepare the demo runtime, sample `.txt` file, and optional screenshots/recording.
- Prepare the Google document and paste the link in the submission task.
- Assign presentation and demo roles.

During the rehearsal:

- Run the presentation exactly like the final defense.
- Time each section.
- Record instructor feedback.
- Mark only small quick fixes for the final pass.
- Do not reopen large feature scope after rehearsal.

After the rehearsal:

- Update the Kanban board so only bug fixes and release preparation tasks remain.
- Apply approved quick fixes.
- Re-run targeted checks for changed areas.
- Keep the Release Candidate stable.

## Final submission checklist

The submitted Google document must include:

1. The final presentation structure and script.
2. The role distribution table.
3. The Plan B section for demo risks.
4. A link to the current Kanban board.
5. A short note that code freeze is active and new features are stopped.
