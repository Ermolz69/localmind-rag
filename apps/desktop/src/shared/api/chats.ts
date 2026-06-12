import type {
  OperationData,
  OperationJsonBody,
  OperationPath,
  OperationQuery,
} from "@shared/contracts";
import { toQueryString } from "./common";
import { request } from "./http";

export const chatsApi = {
  getChats: (params: OperationQuery<"ListChats"> = {}) =>
    request<OperationData<"ListChats">>(
      `/chats${toQueryString({
        cursor: params.cursor,
        limit: params.limit,
      })}`,
    ),

  getChatMessages: (conversationId: OperationPath<"ListChatMessages">["id"]) =>
    request<OperationData<"ListChatMessages">>(
      `/chats/${conversationId}/messages`,
    ),

  createChat: (payload: OperationJsonBody<"CreateChat">) =>
    request<OperationData<"CreateChat">>("/chats", {
      method: "POST",
      body: JSON.stringify(payload),
    }),

  updateChat: (
    conversationId: OperationPath<"UpdateChat">["id"],
    payload: OperationJsonBody<"UpdateChat">,
  ) =>
    request<OperationData<"UpdateChat">>(`/chats/${conversationId}`, {
      method: "PUT",
      body: JSON.stringify(payload),
    }),

  deleteChat: (conversationId: OperationPath<"DeleteChat">["id"]) =>
    request<OperationData<"DeleteChat">>(`/chats/${conversationId}`, {
      method: "DELETE",
    }),

  sendChatMessage: (
    conversationId: OperationPath<"SendChatMessage">["id"],
    content: OperationJsonBody<"SendChatMessage">["content"],
    filters?: OperationJsonBody<"SendChatMessage">["filters"],
  ) =>
    request<OperationData<"SendChatMessage">>(
      `/chats/${conversationId}/messages`,
      {
        method: "POST",
        body: JSON.stringify({ content, filters: filters ?? null }),
      },
    ),
};
