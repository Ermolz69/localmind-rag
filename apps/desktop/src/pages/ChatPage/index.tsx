import { LibraryBig } from "lucide-react";
import { ChatThread, MessageComposer } from "@features/chat-message-send";
import { ConversationList } from "@features/conversation-management";
import { ConfirmDialog, ErrorBanner } from "@shared/ui";
import { SourcePanel } from "@widgets/SourcePanel/SourcePanel";
import { cn } from "@shared/lib/cn";
import { useChatPageViewModel } from "./model/useChatPageViewModel";

export function ChatPage() {
  const chat = useChatPageViewModel();
  const sourceCount = chat.activeSources.length;

  return (
    <section className="flex h-[calc(100dvh-6.5rem)] min-h-0 w-full max-w-full gap-3 overflow-hidden rounded-xl bg-gradient-to-br from-background via-card to-background p-3">
      <div
        className={cn(
          "flex min-h-0 shrink-0 flex-col overflow-hidden transition-all duration-300 ease-in-out",
          chat.isSidebarOpen
            ? "w-[300px] opacity-100"
            : "pointer-events-none w-0 opacity-0",
        )}
      >
        <div className="flex h-full w-[300px] flex-col overflow-hidden">
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

      <div className="relative flex min-h-0 min-w-0 flex-1 flex-col overflow-hidden rounded-lg border border-border bg-card shadow-sm">
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
          onNewQuestion={() => chat.setQuestion("")}
          onUsePrompt={chat.setQuestion}
        />
        <MessageComposer
          value={chat.question}
          disabled={chat.isSendingQuestion || chat.isCreatingConversation}
          isSending={chat.isSendingQuestion}
          error={chat.filterError}
          filters={chat.activeFilterChips}
          buckets={chat.buckets}
          onChange={chat.setQuestion}
          onRemoveFilter={chat.removeActiveFilter}
          onSubmit={() => void chat.sendQuestion()}
          onCancel={chat.cancelSendQuestion}
        />
      </div>

      <aside className="flex min-h-0 w-[320px] shrink-0 flex-col overflow-hidden rounded-lg border border-border bg-card shadow-sm">
        <div className="border-b border-border px-4 py-3">
          <div className="flex items-center justify-between gap-3">
            <div className="flex min-w-0 items-center gap-2">
              <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-md border border-border bg-background text-primary">
                <LibraryBig size={16} aria-hidden />
              </span>
              <div className="min-w-0">
                <h2 className="truncate text-sm font-semibold">Sources</h2>
                <p className="truncate text-xs text-muted-foreground">
                  Evidence for the selected answer
                </p>
              </div>
            </div>
            <span className="shrink-0 rounded-full border border-border bg-muted px-2 py-0.5 text-xs font-medium text-muted-foreground">
              {sourceCount}
            </span>
          </div>
        </div>
        <div className="min-h-0 flex-1 overflow-y-auto p-3">
          <SourcePanel sources={chat.activeSources} />
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
