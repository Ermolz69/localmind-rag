export type NoteDto = {
  id: string;
  title: string;
  markdown: string;
  bucketId: string | null;
  tags?: Record<string, string> | null;
};

export type GetNotesRequest = {
  bucketId?: string | null;
  query?: string | null;
  cursor?: string | null;
  limit?: number;
};

export type CreateNoteRequest = {
  title: string;
  markdown: string;
  bucketId?: string | null;
  tags?: Record<string, string> | null;
};

export type UpdateNoteRequest = {
  title: string;
  markdown: string;
  bucketId?: string | null;
  tags?: Record<string, string> | null;
};
