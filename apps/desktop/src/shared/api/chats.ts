import type {
  OperationData,
  OperationJsonBody,
  OperationPath,
  OperationQuery,
  Schema,
} from "@shared/contracts";
import { toQueryString } from "./common";
import { getApiBaseUrl, request } from "./http";

type RagAnswerChunkDto = {
  text: string;
  sources?: Schema<"RagSourceDto">[] | null;
};

type ApiStreamErrorEnvelope = {
  success: false;
  error?: {
    message?: string;
    code?: string;
  } | null;
};

function isApiStreamErrorEnvelope(
  value: unknown,
): value is ApiStreamErrorEnvelope {
  return (
    typeof value === "object" &&
    value !== null &&
    "success" in value &&
    (value as { success?: unknown }).success === false
  );
}

async function* readSseChunks(
  response: Response,
): AsyncGenerator<RagAnswerChunkDto> {
  const reader = response.body?.getReader();

  if (!reader) {
    throw new Error("Chat stream response body is empty.");
  }

  const decoder = new TextDecoder();
  let buffer = "";

  try {
    while (true) {
      const { done, value } = await reader.read();

      if (done) {
        break;
      }

      buffer += decoder.decode(value, { stream: true });

      const events = buffer.split("\n\n");
      buffer = events.pop() ?? "";

      for (const event of events) {
        const data = event
          .split("\n")
          .filter((line) => line.startsWith("data:"))
          .map((line) => line.slice("data:".length).trim())
          .join("\n");

        if (!data || data === "[DONE]") {
          continue;
        }

        const parsed = JSON.parse(data) as unknown;

        if (isApiStreamErrorEnvelope(parsed)) {
          throw new Error(
            parsed.error?.message ??
              parsed.error?.code ??
              "Chat stream failed.",
          );
        }

        yield parsed as RagAnswerChunkDto;
      }
    }
  } finally {
    reader.releaseLock();
  }
}

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

  streamChatMessage: async function* (
    conversationId: OperationPath<"SendChatMessage">["id"],
    content: OperationJsonBody<"SendChatMessage">["content"],
    filters?: OperationJsonBody<"SendChatMessage">["filters"],
    signal?: AbortSignal,
  ) {
    const response = await fetch(
      `${getApiBaseUrl()}/api/v1/chats/${conversationId}/messages/stream`,
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Accept: "text/event-stream",
        },
        body: JSON.stringify({ content, filters: filters ?? null }),
        signal,
      },
    );

    if (!response.ok) {
      throw new Error(`Chat stream failed: ${response.status}`);
    }

    yield* readSseChunks(response);
  },
};
