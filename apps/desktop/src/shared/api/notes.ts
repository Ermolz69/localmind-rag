import type {
  CreateNoteRequest,
  GetNotesRequest,
  NoteDto,
  UpdateNoteRequest,
} from "@entities/note";
import type { CursorPage } from "./common";
import { toQueryString } from "./common";
import { request } from "./http";

export const notesApi = {
  getNotes: (params: GetNotesRequest = {}) =>
    request<CursorPage<NoteDto>>(
      `/api/notes${toQueryString({
        bucketId: params.bucketId,
        query: params.query,
        cursor: params.cursor,
        limit: params.limit,
      })}`,
    ),
  createNote: (payload: CreateNoteRequest) =>
    request<NoteDto>("/api/notes", {
      method: "POST",
      body: JSON.stringify(payload),
    }),
  updateNote: (id: string, payload: UpdateNoteRequest) =>
    request<void>(`/api/notes/${id}`, {
      method: "PUT",
      body: JSON.stringify(payload),
    }),
  deleteNote: (id: string) =>
    request<void>(`/api/notes/${id}`, {
      method: "DELETE",
    }),
};
