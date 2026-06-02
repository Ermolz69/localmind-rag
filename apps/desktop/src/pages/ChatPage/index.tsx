import { Bot } from "lucide-react";
import { ChatThread, MessageComposer } from "@features/chat-message-send";
import { ConversationList } from "@features/conversation-management";
import { ConfirmDialog, EmptyState, ErrorBanner } from "@shared/ui";
import { SourcePanel } from "@widgets/SourcePanel/SourcePanel";
import { cn } from "@shared/lib/cn";
import { useChatPageViewModel } from "./model/useChatPageViewModel";

export function ChatPage() {
  const chat = useChatPageViewModel();

  return (
    <section className="flex h-[calc(100vh-6rem)] min-h-[500px] w-full gap-4 overflow-hidden">
      <div
        className={cn(
          "flex shrink-0 flex-col overflow-hidden transition-all duration-300 ease-in-out",
          chat.isSidebarOpen
            ? "w-[260px] opacity-100"
            : "pointer-events-none w-0 opacity-0",
        )}
      >
        <div className="flex h-full w-[260px] flex-col overflow-hidden">
          <ConversationList
            conversations={chat.conversations}
            messageCounts={chat.messageCounts}
            selectedConversationId={chat.selectedConversationId}
            newConversationTitle={chat.newConversationTitle}
            isLoading={chat.isLoadingConversations}
            isCreating={chat.isCreatingConversation}
            hasMore={chat.hasMoreChats}
            isLoadingMore={chat.isLoadingMoreChats}
            onTitleChange={chat.setNewConversationTitle}
            onCreate={(title) => {
              void chat.createConversation(title);
              chat.setNewConversationTitle("");
            }}
            onSelect={chat.selectConversation}
            onDelete={chat.setDeleteTargetId}
            onLoadMore={() => void chat.loadMoreChats()}
          />
        </div>
      </div>

      <div className="flex min-h-0 flex-1 flex-col overflow-hidden rounded-md border border-border bg-card">
        <ErrorBanner message={chat.conversationError} />
        <ChatThread
          conversation={chat.selectedConversation}
          messages={chat.selectedMessages}
          activeSourceMessageId={chat.selectedAssistantMessage?.id ?? null}
          isLoadingMessages={chat.isLoadingMessages}
          isSidebarOpen={chat.isSidebarOpen}
          isRenaming={chat.isRenamingConversation}
          threadEndRef={chat.threadEndRef}
          onCreate={() => {
            void chat.createConversation(
              `New chat ${chat.conversations.length + 1}`,
            );
          }}
          onRename={(title) => {
            if (chat.selectedConversationId) {
              void chat.renameConversation(chat.selectedConversationId, title);
            }
          }}
          onToggleSidebar={() => chat.setIsSidebarOpen(!chat.isSidebarOpen)}
          onSelectSourceMessage={chat.setActiveSourceMessageId}
        />
        <MessageComposer
          value={chat.question}
          disabled={chat.isSendingQuestion || chat.isCreatingConversation}
          onChange={chat.setQuestion}
          onSubmit={() => void chat.sendQuestion()}
        />
      </div>

      <aside className="flex min-h-0 w-[260px] shrink-0 flex-col overflow-hidden rounded-md border border-border bg-card">
        <div className="border-b border-border p-4">
          <h2 className="text-sm font-semibold">Sources</h2>
          <p className="mt-1 text-sm text-muted-foreground">
            Snippets from the answer context appear here.
          </p>
        </div>
        <div className="min-h-0 flex-1 overflow-y-auto p-4">
          {chat.activeSources.length > 0 ? (
            <SourcePanel sources={chat.activeSources} />
          ) : (
            <EmptyState
              icon={<Bot size={18} aria-hidden />}
              title="No answer selected"
              description="When the assistant answers, the strongest snippets will be shown here."
            />
          )}
        </div>
      </aside>

      <ConfirmDialog
        open={Boolean(chat.deleteTargetId)}
        title="Delete conversation"
        description="This hides the conversation and its messages from the local chat list."
        confirmLabel="Delete"
        isPending={chat.isDeletingConversation}
        onConfirm={() => void chat.deleteConversation()}
        onClose={() => chat.setDeleteTargetId(null)}
      />
    </section>
  );
}
