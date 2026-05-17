import { useEffect } from "react";
import {
  useConversationMessages,
  useSendChatMessage,
} from "@features/chat-message-send";
import { useConversationList } from "@features/conversation-management";
import { useLocalStorage } from "@shared/lib/hooks";

export function useChatPageViewModel() {
  const [sidebarPreference, setSidebarPreference] = useLocalStorage(
    "localmind.chat.sidebarOpen",
    "false",
  );
  const isSidebarOpen = sidebarPreference === "true";

  const conversations = useConversationList();
  const messages = useConversationMessages(
    conversations.selectedConversationId,
  );

  const send = useSendChatMessage({
    appendMessages: messages.appendMessages,
    createConversation: async (title) => {
      const created = await conversations.createConversation(title);
      if (created) {
        messages.initializeConversationMessages(created.id);
      }

      return created;
    },
    newConversationTitle: conversations.newConversationTitle,
    selectedConversationId: conversations.selectedConversationId,
    setActiveSourceMessageId: messages.setActiveSourceMessageId,
    setSelectedConversationId: conversations.setSelectedConversationId,
    updateMessage: messages.updateMessage,
  });

  useEffect(() => {
    if (conversations.selectedConversationId) {
      void messages.loadMessages(conversations.selectedConversationId);
    }
  }, [conversations.selectedConversationId, messages]);

  useEffect(() => {
    messages.threadEndRef.current?.scrollIntoView({
      behavior: "smooth",
      block: "end",
    });
  }, [
    messages.selectedMessages,
    send.isSendingQuestion,
    conversations.selectedConversationId,
    messages.threadEndRef,
  ]);

  const messageCounts = Object.fromEntries(
    conversations.conversations.map((conversation) => [
      conversation.id,
      conversation.id === conversations.selectedConversationId
        ? messages.selectedMessages.length
        : 0,
    ]),
  );

  async function deleteConversation() {
    const targetId = conversations.deleteTargetId;
    const deleted = await conversations.deleteConversation();
    if (targetId && deleted) {
      messages.removeConversationMessages(targetId);
    }
  }

  function selectConversation(conversationId: string) {
    conversations.selectConversation(conversationId);
    messages.selectConversationSources(conversationId);
  }

  return {
    ...conversations,
    ...messages,
    ...send,
    conversationError:
      conversations.conversationListError ??
      messages.messagesError ??
      send.sendMessageError,
    isSidebarOpen,
    messageCounts,
    deleteConversation,
    selectConversation,
    setIsSidebarOpen: (open: boolean) => setSidebarPreference(String(open)),
  };
}
