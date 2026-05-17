import { useCallback, useEffect, useState } from "react";
import type { ChatConversation } from "@entities/chat";
import { chatsApi, getErrorMessage } from "@shared/api";

export function useConversationList() {
  const [conversations, setConversations] = useState<ChatConversation[]>([]);
  const [selectedConversationId, setSelectedConversationId] = useState<
    string | null
  >(null);
  const [chatCursor, setChatCursor] = useState<string | null>(null);
  const [hasMoreChats, setHasMoreChats] = useState(false);
  const [newConversationTitle, setNewConversationTitle] = useState("");
  const [conversationListError, setConversationListError] = useState<
    string | null
  >(null);
  const [isLoadingConversations, setIsLoadingConversations] = useState(true);
  const [isLoadingMoreChats, setIsLoadingMoreChats] = useState(false);
  const [isCreatingConversation, setIsCreatingConversation] = useState(false);
  const [isRenamingConversation, setIsRenamingConversation] = useState(false);
  const [isDeletingConversation, setIsDeletingConversation] = useState(false);
  const [deleteTargetId, setDeleteTargetId] = useState<string | null>(null);

  const selectedConversation =
    conversations.find(
      (conversation) => conversation.id === selectedConversationId,
    ) ?? null;

  const loadConversations = useCallback(async () => {
    setConversationListError(null);
    setIsLoadingConversations(true);
    try {
      const page = await chatsApi.getChats({ limit: 30 });
      setConversations(page.items);
      setChatCursor(page.nextCursor);
      setHasMoreChats(page.hasMore);
      setSelectedConversationId(
        (current) => current ?? page.items[0]?.id ?? null,
      );
    } catch (exception) {
      setConversationListError(
        getErrorMessage(exception, "Failed to load conversations."),
      );
    } finally {
      setIsLoadingConversations(false);
    }
  }, []);

  useEffect(() => {
    void loadConversations();
  }, [loadConversations]);

  const loadMoreChats = useCallback(async () => {
    if (!chatCursor || isLoadingMoreChats) {
      return;
    }

    setConversationListError(null);
    setIsLoadingMoreChats(true);
    try {
      const page = await chatsApi.getChats({ cursor: chatCursor, limit: 30 });
      setConversations((current) => [...current, ...page.items]);
      setChatCursor(page.nextCursor);
      setHasMoreChats(page.hasMore);
    } catch (exception) {
      setConversationListError(
        getErrorMessage(exception, "Failed to load conversations."),
      );
    } finally {
      setIsLoadingMoreChats(false);
    }
  }, [chatCursor, isLoadingMoreChats]);

  const createConversation = useCallback(async (title: string) => {
    setConversationListError(null);
    setIsCreatingConversation(true);
    try {
      const conversation = await chatsApi.createChat({ title });
      setConversations((current) => [conversation, ...current]);
      setSelectedConversationId(conversation.id);
      return conversation;
    } catch (exception) {
      setConversationListError(
        getErrorMessage(exception, "Failed to create conversation."),
      );
      return null;
    } finally {
      setIsCreatingConversation(false);
    }
  }, []);

  const renameConversation = useCallback(
    async (conversationId: string, title: string) => {
      const nextTitle = title.trim();
      if (!nextTitle) {
        return;
      }

      setConversationListError(null);
      setIsRenamingConversation(true);
      try {
        await chatsApi.updateChat(conversationId, { title: nextTitle });
        setConversations((current) =>
          current.map((conversation) =>
            conversation.id === conversationId
              ? { ...conversation, title: nextTitle }
              : conversation,
          ),
        );
      } catch (exception) {
        setConversationListError(
          getErrorMessage(exception, "Failed to rename conversation."),
        );
      } finally {
        setIsRenamingConversation(false);
      }
    },
    [],
  );

  const deleteConversation = useCallback(async () => {
    if (!deleteTargetId) {
      return;
    }

    setConversationListError(null);
    setIsDeletingConversation(true);
    try {
      await chatsApi.deleteChat(deleteTargetId);
      const nextSelected =
        selectedConversationId === deleteTargetId
          ? (conversations.find((item) => item.id !== deleteTargetId)?.id ??
            null)
          : selectedConversationId;

      setConversations((current) =>
        current.filter((conversation) => conversation.id !== deleteTargetId),
      );
      setSelectedConversationId(nextSelected);
      setDeleteTargetId(null);
      return true;
    } catch (exception) {
      setConversationListError(
        getErrorMessage(exception, "Failed to delete conversation."),
      );
      return false;
    } finally {
      setIsDeletingConversation(false);
    }
  }, [conversations, deleteTargetId, selectedConversationId]);

  const selectConversation = useCallback((conversationId: string) => {
    setSelectedConversationId(conversationId);
  }, []);

  return {
    conversationListError,
    conversations,
    createConversation,
    deleteConversation,
    deleteTargetId,
    hasMoreChats,
    isCreatingConversation,
    isDeletingConversation,
    isLoadingConversations,
    isLoadingMoreChats,
    isRenamingConversation,
    loadMoreChats,
    newConversationTitle,
    renameConversation,
    selectedConversation,
    selectedConversationId,
    selectConversation,
    setDeleteTargetId,
    setNewConversationTitle,
    setSelectedConversationId,
  };
}
