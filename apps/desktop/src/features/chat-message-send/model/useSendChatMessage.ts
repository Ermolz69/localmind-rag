import { useCallback, useState } from "react";
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
import { useApiMutation } from "@shared/lib/hooks";
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

  const sendMutation = useApiMutation(
    (conversationId: string, content: string, filters?: RetrievalFilters) =>
      chatsApi.sendChatMessage(conversationId, content, filters),
    { fallbackError: "The local API request failed." },
  );

  const handleQuestionChange = useCallback(
    (nextValue: string) => {
      setFilterError(null);

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
    if (!content || sendMutation.isPending) {
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

    const answer = await sendMutation.mutate(
      conversationId,
      content,
      hasActiveFilters(filtersForRequest) ? filtersForRequest : undefined,
    );

    if (answer) {
      updateMessage(conversationId, assistantMessageId, (message) => ({
        ...message,
        content: answer.answer,
        status: "ready",
        sources: answer.sources,
      }));
      setActiveSourceMessageId(assistantMessageId);
    } else {
      updateMessage(conversationId, assistantMessageId, (message) => ({
        ...message,
        content: "I couldn't generate an answer for that question.",
        status: "error",
        sources: [],
        error: sendMutation.error ?? "The local API request failed.",
      }));
    }
  }, [
    appendMessages,
    activeFilters,
    buckets,
    createConversation,
    newConversationTitle,
    question,
    selectedConversationId,
    setActiveSourceMessageId,
    setSelectedConversationId,
    updateMessage,
    sendMutation,
  ]);

  function removeActiveFilter(key: SearchFilterKey, tagKey?: string) {
    setFilterError(null);
    setActiveFilters((current) => removeFilter(current, key, tagKey));
  }

  return {
    activeFilterChips: buildFilterChips(activeFilters, buckets),
    filterError,
    isSendingQuestion: sendMutation.isPending,
    question,
    removeActiveFilter,
    sendMessageError: filterError ?? sendMutation.error,
    sendQuestion,
    setQuestion: handleQuestionChange,
  };
}
