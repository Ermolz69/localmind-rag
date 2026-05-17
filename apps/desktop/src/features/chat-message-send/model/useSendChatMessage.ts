import { useCallback, useState } from "react";
import type { ChatConversation } from "@entities/chat";
import { chatsApi, getErrorMessage } from "@shared/api";
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
  const [sendMessageError, setSendMessageError] = useState<string | null>(null);
  const [isSendingQuestion, setIsSendingQuestion] = useState(false);

  const sendQuestion = useCallback(async () => {
    const content = question.trim();
    if (!content || isSendingQuestion) {
      return;
    }

    setSendMessageError(null);
    setIsSendingQuestion(true);

    let conversationId = selectedConversationId;
    if (!conversationId) {
      const created = await createConversation(
        newConversationTitle.trim() || content.slice(0, 48) || "New chat",
      );
      if (!created) {
        setIsSendingQuestion(false);
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

    try {
      const answer = await chatsApi.sendChatMessage(conversationId, content);
      updateMessage(conversationId, assistantMessageId, (message) => ({
        ...message,
        content: answer.answer,
        status: "ready",
        sources: answer.sources,
      }));
      setActiveSourceMessageId(assistantMessageId);
    } catch (exception) {
      const error = getErrorMessage(exception, "The local API request failed.");
      updateMessage(conversationId, assistantMessageId, (message) => ({
        ...message,
        content: "I couldn't generate an answer for that question.",
        status: "error",
        sources: [],
        error,
      }));
      setSendMessageError(error);
    } finally {
      setIsSendingQuestion(false);
    }
  }, [
    appendMessages,
    createConversation,
    isSendingQuestion,
    newConversationTitle,
    question,
    selectedConversationId,
    setActiveSourceMessageId,
    setSelectedConversationId,
    updateMessage,
  ]);

  return {
    isSendingQuestion,
    question,
    sendMessageError,
    sendQuestion,
    setQuestion,
  };
}
