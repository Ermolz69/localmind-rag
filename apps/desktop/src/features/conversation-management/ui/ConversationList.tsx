import { Loader2, Plus, Trash2 } from "lucide-react";
import type { FormEvent } from "react";
import type { ChatConversation } from "@entities/chat";
import { cn } from "@shared/lib/cn";
import { Button } from "@shared/ui";
import { EmptyState } from "@shared/ui";
import { Input } from "@shared/ui";

type ConversationListProps = {
  conversations: ChatConversation[];
  messageCounts: Record<string, number>;
  selectedConversationId: string | null;
  newConversationTitle: string;
  isLoading: boolean;
  isCreating: boolean;
  hasMore: boolean;
  isLoadingMore: boolean;
  onTitleChange: (value: string) => void;
  onCreate: (title: string) => void;
  onSelect: (conversationId: string) => void;
  onDelete: (conversationId: string) => void;
  onLoadMore: () => void;
};

export function ConversationList({
  conversations,
  messageCounts,
  selectedConversationId,
  newConversationTitle,
  isLoading,
  isCreating,
  hasMore,
  isLoadingMore,
  onTitleChange,
  onCreate,
  onSelect,
  onDelete,
  onLoadMore,
}: ConversationListProps) {
  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    onCreate(
      newConversationTitle.trim() || `New chat ${conversations.length + 1}`,
    );
  }

  return (
    <aside className="flex min-h-0 flex-col overflow-hidden rounded-md border border-border bg-card">
      <div className="border-b border-border p-4">
        <h1 className="text-lg font-semibold">Local RAG chat</h1>
        <p className="text-sm text-muted-foreground">
          Conversations backed by the local API.
        </p>
        <form className="mt-4 flex gap-2" onSubmit={submit}>
          <Input
            value={newConversationTitle}
            onChange={(event) => onTitleChange(event.target.value)}
            placeholder="New conversation title"
          />
          <Button type="submit" variant="secondary" disabled={isCreating}>
            {isCreating ? (
              <Loader2 size={16} className="animate-spin" aria-hidden />
            ) : (
              <Plus size={16} aria-hidden />
            )}
            Create
          </Button>
        </form>
      </div>

      <div className="min-h-0 flex-1 overflow-y-auto p-3">
        {isLoading ? (
          <div className="rounded-md border border-dashed border-border p-4 text-sm text-muted-foreground">
            Loading conversations...
          </div>
        ) : conversations.length > 0 ? (
          <div className="space-y-2">
            {conversations.map((conversation) => {
              const isActive = conversation.id === selectedConversationId;
              const messageCount = messageCounts[conversation.id] ?? 0;
              return (
                <div
                  key={conversation.id}
                  className={cn(
                    "group flex items-start gap-2 rounded-md border p-2 transition",
                    isActive
                      ? "border-primary/40 bg-primary/10"
                      : "border-border bg-background hover:bg-muted/50",
                  )}
                >
                  <button
                    type="button"
                    onClick={() => onSelect(conversation.id)}
                    className="min-w-0 flex-1 text-left"
                  >
                    <div className="truncate text-sm font-medium">
                      {conversation.title}
                    </div>
                    <div className="mt-1 text-xs text-muted-foreground">
                      {messageCount > 0
                        ? `${messageCount} message${messageCount === 1 ? "" : "s"}`
                        : "No questions yet"}
                    </div>
                  </button>
                  <button
                    type="button"
                    className="rounded-md p-1 text-muted-foreground transition hover:bg-muted hover:text-foreground"
                    onClick={() => onDelete(conversation.id)}
                    title="Delete conversation"
                  >
                    <Trash2 size={15} aria-hidden />
                  </button>
                </div>
              );
            })}
            {hasMore ? (
              <Button
                className="w-full"
                variant="secondary"
                onClick={onLoadMore}
                disabled={isLoadingMore}
              >
                {isLoadingMore ? "Loading..." : "Load more"}
              </Button>
            ) : null}
          </div>
        ) : (
          <EmptyState
            icon={<Plus size={18} aria-hidden />}
            title="No conversations yet"
            description="Create a conversation to start asking grounded questions against your local index."
            action={
              <Button
                type="button"
                variant="secondary"
                onClick={() => onCreate(`New chat ${conversations.length + 1}`)}
                disabled={isCreating}
              >
                <Plus size={16} aria-hidden />
                New conversation
              </Button>
            }
          />
        )}
      </div>
    </aside>
  );
}
