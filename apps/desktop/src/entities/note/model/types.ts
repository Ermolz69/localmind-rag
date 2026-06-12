import type {
  OperationJsonBody,
  OperationQuery,
  Schema,
} from "@shared/contracts";

export type NoteDto = Schema<"NoteDto">;
export type GetNotesRequest = OperationQuery<"ListNotes">;
export type CreateNoteRequest = OperationJsonBody<"CreateNote">;
export type UpdateNoteRequest = OperationJsonBody<"UpdateNote">;
