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
};
