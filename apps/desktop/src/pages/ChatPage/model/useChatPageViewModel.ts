import { useEffect } from "react";
import {
  useConversationMessages,
  useSendChatMessage,
} from "@features/chat-message-send";
import { useBuckets } from "@features/bucket-management";
import { useConversationList } from "@features/conversation-management";
import { useLocalStorage } from "@shared/lib/hooks";

export function useChatPageViewModel() {
  const [sidebarPreference, setSidebarPreference] = useLocalStorage(
    "localmind.chat.sidebarOpen",
    "false",
  );
  const isSidebarOpen = sidebarPreference === "true";

  const buckets = useBuckets();
  const conversations = useConversationList();
  const messages = useConversationMessages(
    conversations.selectedConversationId,
  );

  const send = useSendChatMessage({
    appendMessages: messages.appendMessages,
    buckets: buckets.buckets,
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
    rememberMessageSources: messages.rememberMessageSources,
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

  const sourceCounts = Object.fromEntries(
    conversations.conversations.map((conversation) => [
      conversation.id,
      messages.sourceCountsByConversation[conversation.id] ?? 0,
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
    buckets: buckets.buckets,
    conversationError:
      conversations.conversationListError ??
      buckets.error ??
      messages.messagesError ??
      send.sendMessageError,
    isSidebarOpen,
    messageCounts: sourceCounts,
    deleteConversation,
    selectConversation,
    setIsSidebarOpen: (open: boolean) => setSidebarPreference(String(open)),
  };
}
