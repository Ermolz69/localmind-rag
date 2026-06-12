import { useCallback, useEffect, useRef, useState } from "react";
import type { BucketDto } from "@entities/bucket";
import type { ChatConversation } from "@entities/chat";
import type { RetrievalFilters, SearchFilterKey } from "@entities/search";
import {
  buildFilterChips,
  hasActiveFilters,
  removeFilter,
} from "@entities/search";
import {
  extractLiveCommands,
  prepareChatSubmission,
} from "@shared/lib/searchFilterCommands";
import { chatsApi } from "@shared/api";
import type { ChatMessageView } from "./useConversationMessages";

type UseSendChatMessageOptions = {
  appendMessages: (conversationId: string, messages: ChatMessageView[]) => void;
  buckets: BucketDto[];
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

function isAbortError(error: unknown) {
  return error instanceof DOMException && error.name === "AbortError";
}

function getErrorMessage(error: unknown) {
  return error instanceof Error
    ? error.message
    : "The local API request failed.";
}

export function useSendChatMessage({
  appendMessages,
  buckets,
  createConversation,
  newConversationTitle,
  selectedConversationId,
  setActiveSourceMessageId,
  setSelectedConversationId,
  updateMessage,
}: UseSendChatMessageOptions) {
  const [question, setQuestion] = useState("");
  const [activeFilters, setActiveFilters] = useState<RetrievalFilters>({});
  const [filterError, setFilterError] = useState<string | null>(null);
  const [sendMessageError, setSendMessageError] = useState<string | null>(null);
  const [isStreaming, setIsStreaming] = useState(false);

  const abortControllerRef = useRef<AbortController | null>(null);

  const cancelSendQuestion = useCallback(() => {
    abortControllerRef.current?.abort();
    abortControllerRef.current = null;
  }, []);

  useEffect(() => {
    return () => {
      abortControllerRef.current?.abort();
    };
  }, []);

  const handleQuestionChange = useCallback(
    (nextValue: string) => {
      setFilterError(null);
      setSendMessageError(null);

      const nextDraft = extractLiveCommands(
        nextValue,
        activeFilters,
        buckets,
        [],
      );

      setActiveFilters(nextDraft.filters);
      setQuestion(nextDraft.content);
    },
    [activeFilters, buckets],
  );

  const sendQuestion = useCallback(async () => {
    const parsed = prepareChatSubmission(question, activeFilters, buckets, []);

    if (parsed.error) {
      setFilterError(parsed.error);
      return;
    }

    if (!parsed.content && parsed.consumedCommand) {
      setActiveFilters(parsed.filters);
      setQuestion("");
      setFilterError(null);
      return;
    }

    const content = parsed.content;

    if (!content || isStreaming) {
      return;
    }

    const filtersForRequest = parsed.filters;
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

    const abortController = new AbortController();
    abortControllerRef.current = abortController;

    setIsStreaming(true);
    setSendMessageError(null);
    setQuestion("");
    setFilterError(null);
    setActiveFilters({});

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

    let streamedText = "";

    try {
      for await (const chunk of chatsApi.streamChatMessage(
        conversationId,
        content,
        hasActiveFilters(filtersForRequest) ? filtersForRequest : undefined,
        abortController.signal,
      )) {
        streamedText += chunk.text ?? "";

        updateMessage(conversationId, assistantMessageId, (message) => ({
          ...message,
          content: streamedText || message.content,
          sources: chunk.sources ?? message.sources,
          status: "pending",
        }));

        if (chunk.sources?.length) {
          setActiveSourceMessageId(assistantMessageId);
        }
      }

      updateMessage(conversationId, assistantMessageId, (message) => ({
        ...message,
        content: streamedText || message.content,
        status: "ready",
      }));

      setActiveSourceMessageId(assistantMessageId);
    } catch (error) {
      if (isAbortError(error)) {
        updateMessage(conversationId, assistantMessageId, (message) => ({
          ...message,
          content: streamedText || "Generation cancelled.",
          status: "ready",
        }));
        return;
      }

      const message = getErrorMessage(error);
      setSendMessageError(message);

      updateMessage(conversationId, assistantMessageId, (current) => ({
        ...current,
        content:
          streamedText || "I couldn't generate an answer for that question.",
        status: "error",
        error: message,
      }));
    } finally {
      if (abortControllerRef.current === abortController) {
        abortControllerRef.current = null;
      }

      setIsStreaming(false);
    }
  }, [
    activeFilters,
    appendMessages,
    buckets,
    createConversation,
    isStreaming,
    newConversationTitle,
    question,
    selectedConversationId,
    setActiveSourceMessageId,
    setSelectedConversationId,
    updateMessage,
  ]);

  function removeActiveFilter(key: SearchFilterKey, tagKey?: string) {
    setFilterError(null);
    setActiveFilters((current) => removeFilter(current, key, tagKey));
  }

  return {
    activeFilterChips: buildFilterChips(activeFilters, buckets),
    cancelSendQuestion,
    filterError,
    isSendingQuestion: isStreaming,
    question,
    removeActiveFilter,
    sendMessageError: filterError ?? sendMessageError,
    sendQuestion,
    setQuestion: handleQuestionChange,
  };
}
