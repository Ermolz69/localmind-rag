import type {
  OperationData,
  OperationJsonBody,
  OperationPath,
  OperationQuery,
} from "@shared/contracts";
import { toQueryString } from "./common";
import { request } from "./http";

export const notesApi = {
  getNotes: (params: OperationQuery<"ListNotes"> = {}) =>
    request<OperationData<"ListNotes">>(
      `/notes${toQueryString({
        bucketId: params.bucketId,
        folderId: params.folderId,
        query: params.query,
        cursor: params.cursor,
        limit: params.limit,
      })}`,
    ),

  createNote: (payload: OperationJsonBody<"CreateNote">) =>
    request<OperationData<"CreateNote">>("/notes", {
      method: "POST",
      body: JSON.stringify(payload),
    }),

  updateNote: (
    id: OperationPath<"UpdateNote">["id"],
    payload: OperationJsonBody<"UpdateNote">,
  ) =>
    request<OperationData<"UpdateNote">>(`/notes/${id}`, {
      method: "PUT",
      body: JSON.stringify(payload),
    }),

  deleteNote: (id: OperationPath<"DeleteNote">["id"]) =>
    request<OperationData<"DeleteNote">>(`/notes/${id}`, {
      method: "DELETE",
    }),

  getNotesTree: (bucketId: OperationPath<"GetNotesTree">["bucketId"]) =>
    request<OperationData<"GetNotesTree">>(`/buckets/${bucketId}/notes/tree`),

  getNoteFolders: (bucketId: OperationPath<"ListNoteFolders">["bucketId"]) =>
    request<OperationData<"ListNoteFolders">>(
      `/buckets/${bucketId}/note-folders`,
    ),

  createNoteFolder: (
    bucketId: OperationPath<"CreateNoteFolder">["bucketId"],
    payload: OperationJsonBody<"CreateNoteFolder">,
  ) =>
    request<OperationData<"CreateNoteFolder">>(
      `/buckets/${bucketId}/note-folders`,
      {
        method: "POST",
        body: JSON.stringify(payload),
      },
    ),

  updateNoteFolder: (
    id: OperationPath<"UpdateNoteFolder">["id"],
    payload: OperationJsonBody<"UpdateNoteFolder">,
  ) =>
    request<OperationData<"UpdateNoteFolder">>(`/note-folders/${id}`, {
      method: "PUT",
      body: JSON.stringify(payload),
    }),

  deleteNoteFolder: (id: OperationPath<"DeleteNoteFolder">["id"]) =>
    request<OperationData<"DeleteNoteFolder">>(`/note-folders/${id}`, {
      method: "DELETE",
    }),

  moveNote: (
    id: OperationPath<"MoveNote">["id"],
    payload: OperationJsonBody<"MoveNote">,
  ) =>
    request<OperationData<"MoveNote">>(`/notes/${id}/move`, {
      method: "POST",
      body: JSON.stringify(payload),
    }),

  moveNoteFolder: (
    id: OperationPath<"MoveNoteFolder">["id"],
    payload: OperationJsonBody<"MoveNoteFolder">,
  ) =>
    request<OperationData<"MoveNoteFolder">>(`/note-folders/${id}/move`, {
      method: "POST",
      body: JSON.stringify(payload),
    }),
};
