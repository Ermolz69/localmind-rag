# Notes and Folders

LocalMind includes a local note-taking system where notes live in buckets and can be organized into a folder hierarchy. Notes support markdown content, tags, and inter-note links.

## Data Model

```
Bucket
  └── NoteFolder (recursive, zero or more levels)
        └── Note
```

A `NoteFolder` belongs to one bucket and optionally has a parent `NoteFolder`. The `ParentFolderId` is `null` for top-level folders. Notes belong to a bucket and optionally to one `NoteFolder`.

The `Conversation` entity tracks two separate timestamps for title state:

- `NoteFolder.ParentFolderId` — `null` for root-level folders in the bucket.
- `Note.NoteFolderId` — `null` for notes that are not inside any folder (bucket root).

## Endpoints

### Folder management

- `GET /api/v1/buckets/{bucketId}/note-folders` — lists all folders in the bucket (flat list with parent ids for client-side tree building).
- `GET /api/v1/buckets/{bucketId}/notes/tree` — returns the complete folder/note hierarchy as a nested tree, ready for rendering without client-side assembly.
- `POST /api/v1/buckets/{bucketId}/note-folders` — creates a new folder; body includes `name` and optional `parentFolderId`.
- `PUT /api/v1/note-folders/{id}` — renames a folder or changes its description.
- `DELETE /api/v1/note-folders/{id}` — deletes a folder. Notes and sub-folders inside the deleted folder are moved to the parent folder or to the bucket root, depending on the implementation policy; they are never silently deleted.
- `POST /api/v1/note-folders/{id}/move` — moves a folder to a new parent; body includes `targetParentFolderId` (null to move to bucket root). Cycles are rejected.

### Note placement

- `POST /api/v1/notes/{id}/move` — moves a note to a different folder or to the bucket root; body includes `targetFolderId` (null for root).

## Tags

Notes support tag labels stored in `note_tags`. Tags are free-form strings. The same tag taxonomy is used for documents (`document_tags`) and chunks (`document_chunk_tags`). Tags are managed per-entity; there is no global tag registry — a tag exists as long as at least one entity references it.

## Note Links

`note_links` records directional relationships between notes (`SourceNoteId` → `TargetNoteId`). Links are displayed in a linked-notes view. Deleting a note removes its outbound and inbound links.

## Tree Endpoint Shape

`GET /api/v1/buckets/{bucketId}/notes/tree` returns a structure suitable for tree rendering:

```json
{
  "folders": [
    {
      "id": "...",
      "name": "Research",
      "parentFolderId": null,
      "children": [
        {
          "id": "...",
          "name": "2026",
          "parentFolderId": "...",
          "children": [],
          "notes": [{ "id": "...", "title": "Q1 summary" }]
        }
      ],
      "notes": []
    }
  ],
  "rootNotes": [{ "id": "...", "title": "Scratch pad" }]
}
```

`rootNotes` contains notes that belong to the bucket but are not inside any folder.

## Invariants

- A folder cannot be moved into one of its own descendants (cycle guard).
- Deleting a bucket cascades to its folders and notes.
- Folder names are unique within the same parent scope (same `ParentFolderId` and same `BucketId`).
- Notes without a `NoteFolderId` appear in `rootNotes` of the tree, not as orphans.
