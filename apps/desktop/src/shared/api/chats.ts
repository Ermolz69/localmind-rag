import type {
  ChatConversation,
  ChatMessageDto,
  CreateConversationRequest,
  RagAnswerDto,
  UpdateConversationRequest,
} from "@entities/chat";

import type { CursorPage, CursorPageRequest } from "./common";
import { toQueryString } from "./common";
import { request } from "./http";

export const chatsApi = {
  getChats: (params: CursorPageRequest = {}) =>
    request<CursorPage<ChatConversation>>(
      `/chats${toQueryString({
        cursor: params.cursor,
        limit: params.limit,
      })}`,
    ),

  getChatMessages: (conversationId: string) =>
    request<ChatMessageDto[]>(`/chats/${conversationId}/messages`),

  createChat: (payload: CreateConversationRequest) =>
    request<ChatConversation>("/chats", {
      method: "POST",
      body: JSON.stringify(payload),
    }),

  updateChat: (conversationId: string, payload: UpdateConversationRequest) =>
    request<void>(`/chats/${conversationId}`, {
      method: "PUT",
      body: JSON.stringify(payload),
    }),

  deleteChat: (conversationId: string) =>
    request<void>(`/chats/${conversationId}`, {
      method: "DELETE",
    }),

  sendChatMessage: (conversationId: string, content: string) =>
    request<RagAnswerDto>(`/chats/${conversationId}/messages`, {
      method: "POST",
      body: JSON.stringify({ content }),
    }),
};
