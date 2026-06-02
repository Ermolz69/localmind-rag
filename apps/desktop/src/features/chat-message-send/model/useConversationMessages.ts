import { useCallback, useMemo, useRef, useState } from "react";
import type { ChatMessageDto } from "@entities/chat";
import type { RagSource } from "@entities/source";
import { chatsApi } from "@shared/api";
import { useApiQuery } from "@shared/lib/hooks";

export type ChatMessageView = {
  id: string;
  role: "user" | "assistant";
  content: string;
  status: "pending" | "ready" | "error";
  sources: RagSource[];
  error?: string;
};

function mapPersistedMessage(message: ChatMessageDto): ChatMessageView {
  return {
    id: message.id,
    role: message.role.toLowerCase() === "assistant" ? "assistant" : "user",
    content: message.content,
    status: "ready",
    sources: [],
  };
}

export function useConversationMessages(selectedConversationId: string | null) {
  const [messagesByConversation, setMessagesByConversation] = useState<
    Record<string, ChatMessageView[]>
  >({});
  const [loadedMessageConversationIds, setLoadedMessageConversationIds] =
    useState<Set<string>>(() => new Set());
  const [activeSourceMessageId, setActiveSourceMessageId] = useState<
    string | null
  >(null);
  const threadEndRef = useRef<HTMLDivElement | null>(null);

  const messagesQuery = useApiQuery(
    () =>
      selectedConversationId
        ? chatsApi.getChatMessages(selectedConversationId)
        : Promise.resolve([]),
    { enabled: false, fallbackError: "Failed to load chat messages." },
  );

  const selectedMessages = useMemo(
    () =>
      selectedConversationId
        ? (messagesByConversation[selectedConversationId] ?? [])
        : [],
    [messagesByConversation, selectedConversationId],
  );

  const selectedAssistantMessage = useMemo(() => {
    const byId = selectedMessages.find(
      (message) =>
        message.id === activeSourceMessageId &&
        message.role === "assistant" &&
        message.sources.length > 0,
    );

    return (
      byId ??
      [...selectedMessages]
        .reverse()
        .find(
          (message) =>
            message.role === "assistant" && message.sources.length > 0,
        ) ??
      null
    );
  }, [activeSourceMessageId, selectedMessages]);

  const loadMessages = useCallback(
    async (conversationId: string, force = false) => {
      if (!force && loadedMessageConversationIds.has(conversationId)) {
        return;
      }

      const messages = await messagesQuery.execute();
      if (messages) {
        setMessagesByConversation((current) => ({
          ...current,
          [conversationId]: messages.map(mapPersistedMessage),
        }));
        setLoadedMessageConversationIds((current) => {
          const next = new Set(current);
          next.add(conversationId);
          return next;
        });
      }
    },
    [loadedMessageConversationIds, messagesQuery],
  );

  const appendMessages = useCallback(
    (conversationId: string, messages: ChatMessageView[]) => {
      setMessagesByConversation((current) => ({
        ...current,
        [conversationId]: [...(current[conversationId] ?? []), ...messages],
      }));
    },
    [],
  );

  const updateMessage = useCallback(
    (
      conversationId: string,
      messageId: string,
      updater: (message: ChatMessageView) => ChatMessageView,
    ) => {
      setMessagesByConversation((current) => ({
        ...current,
        [conversationId]: (current[conversationId] ?? []).map((message) =>
          message.id === messageId ? updater(message) : message,
        ),
      }));
    },
    [],
  );

  const initializeConversationMessages = useCallback(
    (conversationId: string) => {
      setMessagesByConversation((current) => ({
        ...current,
        [conversationId]: current[conversationId] ?? [],
      }));
      setLoadedMessageConversationIds((current) => {
        const next = new Set(current);
        next.add(conversationId);
        return next;
      });
    },
    [],
  );

  const removeConversationMessages = useCallback((conversationId: string) => {
    setMessagesByConversation((current) => {
      const next = { ...current };
      delete next[conversationId];
      return next;
    });
    setLoadedMessageConversationIds((current) => {
      const next = new Set(current);
      next.delete(conversationId);
      return next;
    });
  }, []);

  const selectConversationSources = useCallback(
    (conversationId: string) => {
      const lastAssistantWithSources = [
        ...(messagesByConversation[conversationId] ?? []),
      ]
        .reverse()
        .find(
          (message) =>
            message.role === "assistant" && message.sources.length > 0,
        );
      setActiveSourceMessageId(lastAssistantWithSources?.id ?? null);
    },
    [messagesByConversation],
  );

  return {
    activeSources: selectedAssistantMessage?.sources ?? [],
    activeSourceMessageId,
    appendMessages,
    initializeConversationMessages,
    isLoadingMessages: messagesQuery.isLoading || messagesQuery.isFetching,
    loadMessages,
    messagesError: messagesQuery.error,
    removeConversationMessages,
    selectedAssistantMessage,
    selectedMessages,
    selectConversationSources,
    setActiveSourceMessageId,
    threadEndRef,
    updateMessage,
  };
}
