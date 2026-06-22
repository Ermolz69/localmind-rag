# LocalMind Demo Gallery
This document collects the recorded demo GIFs and explains what each one shows. GIF numbers are renumbered sequentially for the final gallery, so the list has no skipped numbers.
> Each GIF is clickable: the preview plays directly in GitHub, and clicking it opens the hosted viewer page.
## Table of contents
- [0. Demo overview](#0-demo-overview)
- [1. End-to-end video workflows](#1-end-to-end-video-workflows)
- [2. Recorded GIF use cases](#2-recorded-gif-use-cases)
  - [Startup and app state](#startup-and-app-state)
  - [Buckets](#buckets)
  - [Documents and indexing](#documents-and-indexing)
  - [Search](#search)
  - [Notes and annotations](#notes-and-annotations)
  - [Chat and RAG](#chat-and-rag)
  - [Settings and diagnostics](#settings-and-diagnostics)
- [3. Recommended sample files](#3-recommended-sample-files)
- [4. Suggested folder structure](#4-suggested-folder-structure)
- [5. Recorded coverage map](#5-recorded-coverage-map)

## 0. Demo overview

LocalMind is presented as a local-first personal knowledge base:

1. Launch the application and confirm that the local runtime is available.
2. Create or select a workspace/bucket.
3. Add documents manually or through watched folders.
4. Wait for indexing.
5. Search indexed content with semantic and scoped search.
6. Create notes and annotations.
7. Ask questions in RAG chat.
8. Verify answers through source references.

The main message of the demo is simple: LocalMind covers the full local workflow from raw files to searchable, grounded answers.

## 1. End-to-end video workflows

These are the larger videos that should be placed in `demo-assets/e2e/` when exported.

| File | Title | Purpose |
| --- | --- | --- |
| `01-full-knowledge-base-workflow.mp4` | Full knowledge base workflow | Bucket creation, document upload, indexing, preview, search, notes, chat, and sources. |
| `02-quick-document-to-answer.mp4` | Quick document-to-answer workflow | Upload one document and ask a question without manual organization. |
| `03-watched-folders-companion.mp4` | Watched folders and companion workflow | Auto-ingestion from watched folders and companion/mobile preview. |

## 2. Recorded GIF use cases

### Startup and app state

#### GIF-01 — Application launch and runtime status

[![GIF-01 — Application launch and runtime status](https://i.ibb.co/KxcxFbhb/GIF-01.gif)](https://ibb.co/9HmHscgc)

**Use case:** Launch LocalMind  
**Action:** Open the Dashboard and check the Local runtime status.  
**Result:** The user sees that LocalMind and the local runtime are ready to work.

#### GIF-02 — Theme switching

[![GIF-02 — Theme switching](https://i.ibb.co/Nd26RKnc/GIF-02.gif)](https://ibb.co/JwjRZHFD)

**Use case:** Change the UI theme  
**Action:** Open Settings, select Theme, and switch to another appearance mode.  
**Result:** The interface updates immediately and confirms that themes can be changed from Settings.

#### GIF-03 — Developer mode and runtime paths

[![GIF-03 — Developer mode and runtime paths](https://i.ibb.co/Xx85mtk2/GIF-03.gif)](https://ibb.co/WvtzSfpx)

**Use case:** Inspect diagnostics and local paths  
**Action:** Open Settings, enable Developer mode, and reveal runtime paths.  
**Result:** The user can see where LocalMind stores runtime data and diagnostic files.


### Buckets

#### GIF-04 — Create a bucket

[![GIF-04 — Create a bucket](https://i.ibb.co/kvv3Yw4/GIF-04.gif)](https://ibb.co/FCCsGy5)

**Use case:** Create a new knowledge workspace  
**Action:** Enter a bucket name and click New bucket.  
**Result:** A new bucket card appears in the Buckets list.

#### GIF-05 — Search buckets

[![GIF-05 — Search buckets](https://i.ibb.co/1J0ZH52p/GIF-05.gif)](https://ibb.co/YTN2vMjK)

**Use case:** Find a specific bucket  
**Action:** Type part of a bucket name into the bucket search field.  
**Result:** The bucket list is filtered and only matching buckets remain visible.

#### GIF-06 — Rename a bucket

[![GIF-06 — Rename a bucket](https://i.ibb.co/x43M3Ky/GIF-06.gif)](https://ibb.co/8hXjXnJ)

**Use case:** Rename an existing workspace  
**Action:** Open bucket editing, change the name, and save the update.  
**Result:** The bucket is displayed with its new name.

#### GIF-07 — Delete a bucket

[![GIF-07 — Delete a bucket](https://i.ibb.co/b5jqTCCV/GIF-07.gif)](https://ibb.co/CKpNgYYy)

**Use case:** Remove an unused bucket  
**Action:** Click delete, review the confirmation dialog, and confirm the action.  
**Result:** The bucket is removed from the list.


### Documents and indexing

#### GIF-08 — Upload document with button

[![GIF-08 — Upload document with button](https://i.ibb.co/XZN5Bn3n/GIF-09.gif)](https://ibb.co/WWqz9rPr)

**Use case:** Add a document manually  
**Action:** Open Documents, click Upload, and select a file.  
**Result:** The document appears in the list and receives an ingestion job.

#### GIF-09 — Drag-and-drop upload

[![GIF-09 — Drag-and-drop upload](https://i.ibb.co/JFkGTWHr/GIF-10.gif)](https://ibb.co/KzyfgcWV)

**Use case:** Add a document by drag and drop  
**Action:** Drag a file into the Documents dropzone and release it.  
**Result:** LocalMind queues the file for indexing.

#### GIF-10 — Filter documents by bucket

[![GIF-10 — Filter documents by bucket](https://i.ibb.co/vx0d3zpf/GIF-13.gif)](https://ibb.co/d08pPKFX)

**Use case:** Show documents from one workspace  
**Action:** Open the Bucket dropdown and select a specific bucket.  
**Result:** The Documents list shows only files that belong to the selected bucket.

#### GIF-11 — Filter documents by status

[![GIF-11 — Filter documents by status](https://i.ibb.co/PsPbwv6F/GIF-14.gif)](https://ibb.co/bjCyzMKQ)

**Use case:** Find documents by ingestion state  
**Action:** Open the Status dropdown and select a status filter such as Indexed, Failed, or Processing.  
**Result:** The list updates to show documents that match the selected status.

#### GIF-12 — Document preview

[![GIF-12 — Document preview](https://i.ibb.co/SDQ9m11M/GIF-15.gif)](https://ibb.co/sp5M1DDN)

**Use case:** Preview document content  
**Action:** Click Preview on a document and open the preview modal.  
**Result:** The user can inspect the document without leaving LocalMind.


### Search

#### GIF-13 — Semantic search

[![GIF-13 — Semantic search](https://i.ibb.co/NnS2kLbZ/GIF-18.gif)](https://ibb.co/h1fFnKwD)

**Use case:** Search by meaning  
**Action:** Select Semantic Search, enter a natural-language query, and run the search.  
**Result:** LocalMind returns relevant snippets with document names, page numbers, and scores.

#### GIF-14 — Search filter by bucket

[![GIF-14 — Search filter by bucket](https://i.ibb.co/ZzX572gQ/GIF-20.gif)](https://ibb.co/JjtVg3pY)

**Use case:** Limit search to one bucket  
**Action:** Enter a query, select a bucket filter, and run the search.  
**Result:** Search results are scoped to the selected workspace.

#### GIF-15 — Search filters with slash commands

[![GIF-15 — Search filters with slash commands](https://i.ibb.co/RpMFTZtS/GIF-21.gif)](https://ibb.co/JRb1j08d)

**Use case:** Apply filters quickly  
**Action:** Type /, choose a suggested filter, and apply it as a chip.  
**Result:** The user sees a fast keyboard-driven way to narrow search or chat context.


### Notes and annotations

#### GIF-16 — Choose notes vault or bucket

[![GIF-16 — Choose notes vault or bucket](https://i.ibb.co/bgpmXtT0/GIF-22.gif)](https://ibb.co/SXqyBSbj)

**Use case:** Open notes in a workspace context  
**Action:** Open Notes and choose a bucket in Vault Scope.  
**Result:** The notes explorer displays the note tree for the selected bucket.

#### GIF-17 — Create a notes folder

[![GIF-17 — Create a notes folder](https://i.ibb.co/Qj7hbBb9/GIF-23.gif)](https://ibb.co/YT4JR9R2)

**Use case:** Organize notes  
**Action:** Click New Folder, enter a folder name, and confirm.  
**Result:** A new folder appears in the notes explorer.

#### GIF-18 — Create a markdown note

[![GIF-18 — Create a markdown note](https://i.ibb.co/Xxrwcw8s/GIF-24.gif)](https://ibb.co/0yRwHwtM)

**Use case:** Create a local markdown note  
**Action:** Click New File, enter a note name, and open it.  
**Result:** The note opens in the editor and is ready for writing.

#### GIF-19 — Edit and save a note

[![GIF-19 — Edit and save a note](https://i.ibb.co/1J75Kq9D/GIF-25.gif)](https://ibb.co/84Np25jW)

**Use case:** Write an annotation  
**Action:** Enter markdown text, notice the dirty state, and click Save.  
**Result:** The note is saved locally.

#### GIF-20 — Note properties

[![GIF-20 — Note properties](https://i.ibb.co/TBFWS39f/GIF-27.gif)](https://ibb.co/GvbHLSz6)

**Use case:** Inspect note metadata  
**Action:** Open the note context menu and select Properties.  
**Result:** The properties panel shows metadata and note-related settings.


### Chat and RAG

#### GIF-21 — Create a new chat

[![GIF-21 — Create a new chat](https://i.ibb.co/yBx56wd1/GIF-29.gif)](https://ibb.co/tP1cq7Qt)

**Use case:** Start a separate conversation  
**Action:** Open Chat and create a new conversation.  
**Result:** A new chat is created and ready for questions.

#### GIF-22 — Ask a question about a document

[![GIF-22 — Ask a question about a document](https://i.ibb.co/9HS9pqm5/GIF-30.gif)](https://ibb.co/B2FVfgHM)

**Use case:** Use RAG over local documents  
**Action:** Type a question, send it, and wait for the generated answer.  
**Result:** LocalMind answers using the local knowledge base.

#### GIF-23 — Sources panel

[![GIF-23 — Sources panel](https://i.ibb.co/939RR70M/GIF-31.gif)](https://ibb.co/M5DqqmHX)

**Use case:** Verify the answer  
**Action:** Select an assistant message and open the Sources panel.  
**Result:** The answer is grounded with document snippets and source references.

#### GIF-24 — Chat filter by bucket

[![GIF-24 — Chat filter by bucket](https://i.ibb.co/N2163r9V/GIF-32.gif)](https://ibb.co/4RsZf28m)

**Use case:** Ask questions inside one bucket  
**Action:** Type /bucket, choose a bucket suggestion, and ask a question.  
**Result:** The chat is scoped to the selected bucket using a visible filter chip.

#### GIF-25 — Rename or delete a conversation

[![GIF-25 — Rename or delete a conversation](https://i.ibb.co/pvH4rHnV/GIF-34.gif)](https://ibb.co/3yxSYxk6)

**Use case:** Manage chat history  
**Action:** Select a conversation, rename it, then open the delete dialog.  
**Result:** The user sees how to organize or remove conversations.


### Settings and diagnostics

#### GIF-26 — AI settings

[![GIF-26 — AI settings](https://i.ibb.co/JjPSjPXB/GIF-35.gif)](https://ibb.co/1Gx4GxHv)

**Use case:** Configure local AI runtime  
**Action:** Open Settings and review AI provider, chat model, and embedding model settings.  
**Result:** The user understands where LocalMind AI configuration is managed.

#### GIF-27 — Rescan watched folder

[![GIF-27 — Rescan watched folder](https://i.ibb.co/ZRWfkWSj/GIF-37.gif)](https://ibb.co/20gFfgS9)

**Use case:** Refresh watched folder content  
**Action:** Click Rescan and wait for the scan status/result.  
**Result:** LocalMind rescans the watched folder and updates the last scan result.

#### GIF-28 — Cleanup deleted files

[![GIF-28 — Cleanup deleted files](https://i.ibb.co/q3G0KByk/GIF-38.gif)](https://ibb.co/DHXGh4V5)

**Use case:** Clean removed watched files  
**Action:** Click Cleanup deleted files and confirm the cleanup dialog.  
**Result:** Deleted watched records are removed from LocalMind.

#### GIF-29 — Diagnostics logs

[![GIF-29 — Diagnostics logs](https://i.ibb.co/jvj6DYZh/GIF-39.gif)](https://ibb.co/HD9Fqyp7)

**Use case:** Inspect diagnostic logs  
**Action:** Enable Developer mode, review log settings, and show the Clear logs action.  
**Result:** The user knows where to inspect and maintain diagnostics data.


## 3. Recommended sample files

Use neutral demo files only. Do not record personal documents, real local paths, private notes, credentials, or customer data.

| File | Purpose |
| --- | --- |
| `ai-research-summary.pdf` | Semantic search, page numbers, and source references. |
| `project-notes.md` | Quick upload, content search, and note-like material. |
| `coursework-requirements.docx` | Office document indexing support. |
| `presentation-outline.pptx` | Slide-based indexing support. |
| `broken-document.pdf` | Failed ingestion, retry, and error message demo. |

## 4. Suggested folder structure

```txt
demo-assets/
  e2e/
    01-full-knowledge-base-workflow.mp4
    02-quick-document-to-answer.mp4
    03-watched-folders-companion.mp4

  gifs/
    startup/
    buckets/
    documents/
    search/
    notes/
    chat/
    settings/
    companion/

  sample-files/
    ai-research-summary.pdf
    project-notes.md
    coursework-requirements.docx
    presentation-outline.pptx
    broken-document.pdf
```

## 5. Recorded coverage map

| Feature area | Recorded GIFs |
| --- | --- |
| Startup and app state | `GIF-01`, `GIF-02`, `GIF-03` |
| Buckets | `GIF-04`, `GIF-05`, `GIF-06`, `GIF-07` |
| Documents and indexing | `GIF-08`, `GIF-09`, `GIF-10`, `GIF-11`, `GIF-12` |
| Search | `GIF-13`, `GIF-14`, `GIF-15` |
| Notes and annotations | `GIF-16`, `GIF-17`, `GIF-18`, `GIF-19`, `GIF-20` |
| Chat and RAG | `GIF-21`, `GIF-22`, `GIF-23`, `GIF-24`, `GIF-25` |
| Settings and diagnostics | `GIF-26`, `GIF-27`, `GIF-28`, `GIF-29` |
