import { useCallback, useEffect, useMemo, useRef, useState } from "react";
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

type SourceCacheEntry = {
  content: string;
  sources: RagSource[];
};

type SourceCache = Record<string, SourceCacheEntry[]>;

const sourceCacheStorageKey = "localmind.chat.messageSources.v1";

function normalizeContent(content: string) {
  return content.trim().replace(/\s+/g, " ");
}

function readSourceCache(): SourceCache {
  if (typeof window === "undefined") {
    return {};
  }

  const raw = window.localStorage.getItem(sourceCacheStorageKey);
  if (!raw) {
    return {};
  }

  try {
    const parsed = JSON.parse(raw) as SourceCache;
    return parsed && typeof parsed === "object" ? parsed : {};
  } catch {
    return {};
  }
}

function writeSourceCache(cache: SourceCache) {
  if (typeof window === "undefined") {
    return;
  }

  window.localStorage.setItem(sourceCacheStorageKey, JSON.stringify(cache));
}

function mapPersistedMessage(message: ChatMessageDto): ChatMessageView {
  return {
    id: message.id,
    role: message.role.toLowerCase() === "assistant" ? "assistant" : "user",
    content: message.content,
    status: "ready",
    sources: [],
  };
}

function findCachedSources(
  conversationId: string,
  content: string,
  cache: SourceCache,
) {
  const normalizedContent = normalizeContent(content);
  return (
    cache[conversationId]?.find(
      (entry) => normalizeContent(entry.content) === normalizedContent,
    )?.sources ?? []
  );
}

function mergePersistedMessages(
  conversationId: string,
  messages: ChatMessageDto[],
  existingMessages: ChatMessageView[],
  cache: SourceCache,
) {
  return messages.map((message) => {
    const mapped = mapPersistedMessage(message);
    if (mapped.role !== "assistant") {
      return mapped;
    }

    const existingSources =
      existingMessages.find(
        (existing) =>
          existing.role === "assistant" &&
          normalizeContent(existing.content) ===
            normalizeContent(mapped.content),
      )?.sources ?? [];

    const cachedSources = findCachedSources(
      conversationId,
      mapped.content,
      cache,
    );

    return {
      ...mapped,
      sources: existingSources.length > 0 ? existingSources : cachedSources,
    };
  });
}

function countSources(
  messages: ChatMessageView[] | undefined,
  cacheEntries: SourceCacheEntry[] | undefined,
) {
  const chunkIds = new Set<string>();

  for (const message of messages ?? []) {
    for (const source of message.sources) {
      chunkIds.add(source.chunkId);
    }
  }

  for (const entry of cacheEntries ?? []) {
    for (const source of entry.sources) {
      chunkIds.add(source.chunkId);
    }
  }

  return chunkIds.size;
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
  const [sourceCacheByConversation, setSourceCacheByConversation] =
    useState<SourceCache>(() => readSourceCache());
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

  const selectedCachedSources = selectedConversationId
    ? (sourceCacheByConversation[selectedConversationId]?.at(-1)?.sources ?? [])
    : [];

  const sourceCountsByConversation = useMemo(() => {
    const conversationIds = new Set([
      ...Object.keys(messagesByConversation),
      ...Object.keys(sourceCacheByConversation),
    ]);
    const counts: Record<string, number> = {};

    for (const conversationId of conversationIds) {
      counts[conversationId] = countSources(
        messagesByConversation[conversationId],
        sourceCacheByConversation[conversationId],
      );
    }

    return counts;
  }, [messagesByConversation, sourceCacheByConversation]);

  useEffect(() => {
    writeSourceCache(sourceCacheByConversation);
  }, [sourceCacheByConversation]);

  const loadMessages = useCallback(
    async (conversationId: string, force = false) => {
      if (!force && loadedMessageConversationIds.has(conversationId)) {
        return;
      }

      const messages = await messagesQuery.execute();
      if (messages) {
        setMessagesByConversation((current) => {
          const existingMessages = current[conversationId] ?? [];
          return {
            ...current,
            [conversationId]: mergePersistedMessages(
              conversationId,
              messages,
              existingMessages,
              sourceCacheByConversation,
            ),
          };
        });
        setLoadedMessageConversationIds((current) => {
          const next = new Set(current);
          next.add(conversationId);
          return next;
        });
      }
    },
    [loadedMessageConversationIds, messagesQuery, sourceCacheByConversation],
  );

  const rememberMessageSources = useCallback(
    (conversationId: string, content: string, sources: RagSource[]) => {
      const normalizedContent = normalizeContent(content);
      if (!normalizedContent || sources.length === 0) {
        return;
      }

      setSourceCacheByConversation((current) => {
        const currentEntries = current[conversationId] ?? [];
        const nextEntries = [
          ...currentEntries.filter(
            (entry) => normalizeContent(entry.content) !== normalizedContent,
          ),
          { content, sources },
        ].slice(-50);

        return {
          ...current,
          [conversationId]: nextEntries,
        };
      });
    },
    [],
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
    setSourceCacheByConversation((current) => {
      const next = { ...current };
      delete next[conversationId];
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

  const assistantSources = selectedAssistantMessage?.sources ?? [];

  return {
    activeSources:
      assistantSources.length > 0 ? assistantSources : selectedCachedSources,
    activeSourceMessageId,
    appendMessages,
    initializeConversationMessages,
    isLoadingMessages: messagesQuery.isLoading || messagesQuery.isFetching,
    loadMessages,
    messagesError: messagesQuery.error,
    removeConversationMessages,
    rememberMessageSources,
    selectedAssistantMessage,
    selectedMessages,
    selectConversationSources,
    setActiveSourceMessageId,
    sourceCountsByConversation,
    threadEndRef,
    updateMessage,
  };
}
