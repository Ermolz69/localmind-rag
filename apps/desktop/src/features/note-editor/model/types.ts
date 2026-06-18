export type NoteDraft = {
  title: string;
  markdown: string;
  bucketId: string | null;
  folderId: string | null;
};

export type OpenNoteTab = {
  noteId: string;
  title: string;
  isDirty: boolean;
};

export type EditorViewMode = "source" | "live-preview" | "reading";
