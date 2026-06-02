import { useCallback, useState } from "react";
import type { ChatConversation } from "@entities/chat";
import { chatsApi } from "@shared/api";
import { useApiMutation } from "@shared/lib/hooks";
import type { ChatMessageView } from "./useConversationMessages";

type UseSendChatMessageOptions = {
  appendMessages: (conversationId: string, messages: ChatMessageView[]) => void;
  createConversation: (title: string) => Promise<ChatConversation | null>;
  newConversationTitle: string;
  selectedConversationId: string | null;
  setActiveSourceMessageId: (messageId: string | null) => void;
  setSelectedConversationId: (conversationId: string | null) => void;
  updateMessage: (
    conversationId: string,
    messageId: string,
    updater: (message: ChatMessageView) => ChatMessageView,
  ) => void;
};

export function useSendChatMessage({
  appendMessages,
  createConversation,
  newConversationTitle,
  selectedConversationId,
  setActiveSourceMessageId,
  setSelectedConversationId,
  updateMessage,
}: UseSendChatMessageOptions) {
  const [question, setQuestion] = useState("");

  const sendMutation = useApiMutation(
    (conversationId: string, content: string) =>
      chatsApi.sendChatMessage(conversationId, content),
    { fallbackError: "The local API request failed." },
  );

  const sendQuestion = useCallback(async () => {
    const content = question.trim();
    if (!content || sendMutation.isPending) {
      return;
    }

    let conversationId = selectedConversationId;
    if (!conversationId) {
      const created = await createConversation(
        newConversationTitle.trim() || content.slice(0, 48) || "New chat",
      );
      if (!created) {
        return;
      }

      conversationId = created.id;
    }

    setQuestion("");
    const userMessageId = crypto.randomUUID();
    const assistantMessageId = crypto.randomUUID();
    appendMessages(conversationId, [
      {
        id: userMessageId,
        role: "user",
        content,
        status: "ready",
        sources: [],
      },
      {
        id: assistantMessageId,
        role: "assistant",
        content: "Thinking through your documents...",
        status: "pending",
        sources: [],
      },
    ]);
    setSelectedConversationId(conversationId);
    setActiveSourceMessageId(assistantMessageId);

    const answer = await sendMutation.mutate(conversationId, content);

    if (answer) {
      updateMessage(conversationId, assistantMessageId, (message) => ({
        ...message,
        content: answer.answer,
        status: "ready",
        sources: answer.sources,
      }));
      setActiveSourceMessageId(assistantMessageId);
    } else if (sendMutation.error) {
      updateMessage(conversationId, assistantMessageId, (message) => ({
        ...message,
        content: "I couldn't generate an answer for that question.",
        status: "error",
        sources: [],
        error: sendMutation.error ?? undefined,
      }));
    }
  }, [
    appendMessages,
    createConversation,
    newConversationTitle,
    question,
    selectedConversationId,
    setActiveSourceMessageId,
    setSelectedConversationId,
    updateMessage,
    sendMutation,
  ]);

  return {
    isSendingQuestion: sendMutation.isPending,
    question,
    sendMessageError: sendMutation.error,
    sendQuestion,
    setQuestion,
  };
}
