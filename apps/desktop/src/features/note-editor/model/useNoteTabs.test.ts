import { describe, expect, it } from "vitest";
import type { NoteDto } from "@entities/note";
import {
  getCloseTabDecision,
  getOpenTabDecision,
} from "./useNoteTabs";
import type { OpenNoteTab } from "./types";

function note(id: string, title = id): NoteDto {
  return {
    id,
    title,
    markdown: "",
    bucketId: "bucket-id",
    folderId: null,
    syncStatus: 0,
    tags: null,
    createdAt: "2026-06-18T00:00:00Z",
    updatedAt: null,
  };
}

describe("note tab decisions", () => {
  it("focuses an already-open note instead of duplicating it", () => {
    const tabs: OpenNoteTab[] = [
      { noteId: "note-a", title: "A", isDirty: false },
      { noteId: "note-b", title: "B", isDirty: false },
    ];

    expect(getOpenTabDecision(tabs, "note-a", note("note-b"))).toEqual({
      type: "focus",
      noteId: "note-b",
    });
  });

  it("focuses an existing tab even when opening in a new tab", () => {
    const tabs: OpenNoteTab[] = [
      { noteId: "note-a", title: "A", isDirty: false },
    ];

    expect(
      getOpenTabDecision(tabs, "note-a", note("note-a"), {
        preferNewTab: true,
      }),
    ).toEqual({
      type: "focus",
      noteId: "note-a",
    });
  });

  it("requires confirmation before replacing a dirty active tab", () => {
    const tabs: OpenNoteTab[] = [
      { noteId: "note-a", title: "Dirty note", isDirty: true },
    ];

    expect(getOpenTabDecision(tabs, "note-a", note("note-b"))).toEqual({
      type: "replace",
      activeTabId: "note-a",
      requiresConfirmation: true,
      title: "Dirty note",
    });
  });

  it("requires confirmation before closing a dirty tab", () => {
    const tabs: OpenNoteTab[] = [
      { noteId: "note-a", title: "Dirty note", isDirty: true },
    ];

    expect(getCloseTabDecision(tabs, "note-a")).toEqual({
      type: "close",
      requiresConfirmation: true,
      title: "Dirty note",
    });
  });
});
