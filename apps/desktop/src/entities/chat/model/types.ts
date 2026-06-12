import type { OperationJsonBody, Schema } from "@shared/contracts";

export type ChatConversation = Schema<"ConversationDto">;
export type ChatMessageDto = Schema<"ChatMessageDto">;
export type CreateConversationRequest = OperationJsonBody<"CreateChat">;
export type UpdateConversationRequest = OperationJsonBody<"UpdateChat">;
export type RetrievalFilters = Schema<"RetrievalFilters">;
export type RagAnswerDto = Schema<"RagAnswerDto">;
