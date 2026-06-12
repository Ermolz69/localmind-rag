import { useCallback, useEffect, useState } from "react";
import type { ChatConversation } from "@entities/chat";
import { chatsApi } from "@shared/api";
import { useApiMutation, useCursorPage } from "@shared/lib/hooks";

export function useConversationList() {
  const [selectedConversationId, setSelectedConversationId] = useState<
    string | null
  >(null);
  const [newConversationTitle, setNewConversationTitle] = useState("");
  const [deleteTargetId, setDeleteTargetId] = useState<string | null>(null);

  const {
    items: conversations,
    setItems: setConversations,
    isLoading: isLoadingConversations,
    isLoadingMore: isLoadingMoreChats,
    hasMore: hasMoreChats,
    error: queryError,
    loadMore: loadMoreChats,
  } = useCursorPage<ChatConversation>(
    (cursor) => chatsApi.getChats({ cursor: cursor ?? undefined, limit: 30 }),
    "Failed to load conversations.",
  );

  const selectedConversation =
    conversations.find(
      (conversation) => conversation.id === selectedConversationId,
    ) ?? null;

  useEffect(() => {
    if (!selectedConversationId && conversations.length > 0) {
      setSelectedConversationId(conversations[0].id);
    }
  }, [conversations, selectedConversationId]);

  const createMutation = useApiMutation(
    (title: string) => chatsApi.createChat({ title }),
    { fallbackError: "Failed to create conversation." },
  );

  const createConversation = useCallback(
    async (title: string) => {
      const conversation = await createMutation.mutate(title);
      if (conversation) {
        setConversations((current) => [conversation, ...current]);
        setSelectedConversationId(conversation.id);
        return conversation;
      }
      return null;
    },
    [createMutation, setConversations],
  );

  const renameMutation = useApiMutation(
    (conversationId: string, title: string) =>
      chatsApi.updateChat(conversationId, { title }),
    { fallbackError: "Failed to rename conversation." },
  );

  const renameConversation = useCallback(
    async (conversationId: string, title: string) => {
      const nextTitle = title.trim();
      if (!nextTitle) {
        return;
      }

      const success = await renameMutation.mutate(conversationId, nextTitle);
      if (success !== null) {
        setConversations((current) =>
          current.map((conversation) =>
            conversation.id === conversationId
              ? { ...conversation, title: nextTitle }
              : conversation,
          ),
        );
      }
    },
    [renameMutation, setConversations],
  );

  const deleteMutation = useApiMutation(
    (id: string) => chatsApi.deleteChat(id),
    { fallbackError: "Failed to delete conversation." },
  );

  const deleteConversation = useCallback(async () => {
    if (!deleteTargetId) {
      return false;
    }

    const success = await deleteMutation.mutate(deleteTargetId);
    if (success !== null) {
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
    }
    return false;
  }, [
    conversations,
    deleteMutation,
    deleteTargetId,
    selectedConversationId,
    setConversations,
  ]);

  const selectConversation = useCallback((conversationId: string) => {
    setSelectedConversationId(conversationId);
  }, []);

  return {
    conversationListError:
      queryError ??
      createMutation.error ??
      renameMutation.error ??
      deleteMutation.error,
    conversations,
    createConversation,
    deleteConversation,
    deleteTargetId,
    hasMoreChats,
    isCreatingConversation: createMutation.isPending,
    isDeletingConversation: deleteMutation.isPending,
    isLoadingConversations,
    isLoadingMoreChats,
    isRenamingConversation: renameMutation.isPending,
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
