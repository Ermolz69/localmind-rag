import {
  Archive,
  ChevronDown,
  Loader2,
  MessageSquareText,
  MoreVertical,
  Plus,
  Search,
} from "lucide-react";
import { useMemo, useState, type FormEvent } from "react";
import type { ChatConversation } from "@entities/chat";
import { cn } from "@shared/lib/cn";
import { Button, EmptyState, Input } from "@shared/ui";

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

type ConversationGroup = {
  label: "Today" | "Yesterday" | "Earlier";
  conversations: ChatConversation[];
};

function getConversationDate(conversation: ChatConversation) {
  return new Date(conversation.updatedAt ?? conversation.createdAt);
}

function isSameCalendarDay(left: Date, right: Date) {
  return (
    left.getFullYear() === right.getFullYear() &&
    left.getMonth() === right.getMonth() &&
    left.getDate() === right.getDate()
  );
}

function getConversationGroup(conversation: ChatConversation) {
  const date = getConversationDate(conversation);
  const today = new Date();
  const yesterday = new Date();
  yesterday.setDate(today.getDate() - 1);

  if (isSameCalendarDay(date, today)) {
    return "Today";
  }

  if (isSameCalendarDay(date, yesterday)) {
    return "Yesterday";
  }

  return "Earlier";
}

function formatConversationTimestamp(conversation: ChatConversation) {
  const group = getConversationGroup(conversation);
  const date = getConversationDate(conversation);

  if (group === "Today") {
    return new Intl.DateTimeFormat(undefined, {
      hour: "2-digit",
      minute: "2-digit",
    }).format(date);
  }

  if (group === "Yesterday") {
    return "Yesterday";
  }

  return new Intl.DateTimeFormat(undefined, {
    month: "short",
    day: "numeric",
  }).format(date);
}

function formatSourceCount(count: number) {
  if (count <= 0) {
    return "No sources";
  }

  return `${count} source${count === 1 ? "" : "s"}`;
}

function groupConversations(conversations: ChatConversation[]) {
  const groups: ConversationGroup[] = [
    { label: "Today", conversations: [] },
    { label: "Yesterday", conversations: [] },
    { label: "Earlier", conversations: [] },
  ];

  for (const conversation of conversations) {
    const label = getConversationGroup(conversation);
    groups
      .find((group) => group.label === label)
      ?.conversations.push(conversation);
  }

  return groups.filter((group) => group.conversations.length > 0);
}

function formatArchivedCount(value: number) {
  if (!value) {
    return "0";
  }

  return String(value);
}

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
  const [searchQuery, setSearchQuery] = useState("");

  const filteredConversations = useMemo(() => {
    const normalized = searchQuery.trim().toLowerCase();
    if (!normalized) {
      return conversations;
    }

    return conversations.filter((conversation) =>
      conversation.title.toLowerCase().includes(normalized),
    );
  }, [conversations, searchQuery]);
  const groupedConversations = useMemo(
    () => groupConversations(filteredConversations),
    [filteredConversations],
  );

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    onCreate(
      newConversationTitle.trim() || `New chat ${conversations.length + 1}`,
    );
  }

  return (
    <aside className="flex h-full min-h-0 flex-col overflow-hidden rounded-xl border border-border bg-card/95 shadow-xl backdrop-blur">
      <div className="border-b border-border bg-gradient-to-b from-muted/30 to-card px-3 py-3">
        <div className="flex items-center gap-3">
          <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg border border-primary/30 bg-primary/10 text-primary shadow-sm">
            <MessageSquareText size={17} aria-hidden />
          </div>
          <div className="min-w-0 flex-1">
            <h1 className="truncate text-sm font-semibold">Chats</h1>
            <p className="truncate text-xs text-muted-foreground">
              Local workspace
            </p>
          </div>
          <span className="rounded-full border border-border bg-background/80 px-2 py-0.5 text-xs font-medium text-muted-foreground">
            {conversations.length}
          </span>
        </div>

        <form className="mt-3 flex gap-2" onSubmit={submit}>
          <Input
            className="h-9 text-sm"
            value={newConversationTitle}
            onChange={(event) => onTitleChange(event.target.value)}
            placeholder="New chat"
          />
          <Button
            type="submit"
            className="h-9 w-9 shrink-0 rounded-lg p-0 shadow-sm"
            disabled={isCreating}
            title="Create chat"
          >
            {isCreating ? (
              <Loader2 size={16} className="animate-spin" aria-hidden />
            ) : (
              <Plus size={16} aria-hidden />
            )}
          </Button>
        </form>

        <div className="mt-2 flex items-center gap-2 rounded-lg border border-border bg-background/70 px-3 py-2 shadow-inner">
          <Search size={15} className="shrink-0 text-muted-foreground" />
          <input
            className="min-w-0 flex-1 bg-transparent text-sm text-foreground outline-none placeholder:text-muted-foreground"
            value={searchQuery}
            onChange={(event) => setSearchQuery(event.target.value)}
            placeholder="Search chats"
          />
        </div>
      </div>

      <div className="min-h-0 flex-1 overflow-y-auto px-2 py-3">
        {isLoading ? (
          <div className="space-y-2 px-1">
            <div className="h-16 animate-pulse rounded-lg bg-muted" />
            <div className="h-16 animate-pulse rounded-lg bg-muted" />
            <div className="h-16 animate-pulse rounded-lg bg-muted" />
          </div>
        ) : groupedConversations.length > 0 ? (
          <div className="space-y-4">
            {groupedConversations.map((group) => (
              <section key={group.label} className="space-y-1.5">
                <div className="px-1.5 text-[11px] font-medium text-muted-foreground">
                  {group.label}
                </div>
                {group.conversations.map((conversation) => {
                  const isActive = conversation.id === selectedConversationId;
                  const sourceCount = messageCounts[conversation.id] ?? 0;
                  const timestamp = formatConversationTimestamp(conversation);

                  return (
                    <div
                      key={conversation.id}
                      className={cn(
                        "group relative overflow-hidden rounded-lg border transition",
                        isActive
                          ? "border-primary/70 bg-primary/10 shadow-lg shadow-primary/10 ring-1 ring-primary/30"
                          : "border-border/40 bg-background/20 hover:border-primary/30 hover:bg-muted/30",
                      )}
                    >
                      <button
                        type="button"
                        onClick={() => onSelect(conversation.id)}
                        className="flex min-w-0 flex-1 items-center gap-2.5 px-2.5 py-2.5 pr-9 text-left"
                      >
                        <span
                          className={cn(
                            "flex h-7 w-7 shrink-0 items-center justify-center rounded-md border text-muted-foreground",
                            isActive
                              ? "border-primary/40 bg-background text-primary"
                              : "border-border bg-card/70",
                          )}
                        >
                          <MessageSquareText size={14} aria-hidden />
                        </span>
                        <span className="min-w-0 flex-1">
                          <span className="block truncate text-xs font-semibold text-foreground">
                            {conversation.title}
                          </span>
                          <span className="mt-1 flex min-w-0 items-center justify-between gap-2 text-[11px] text-muted-foreground">
                            <span className="truncate">
                              {formatSourceCount(sourceCount)}
                            </span>
                            <span className="shrink-0">{timestamp}</span>
                          </span>
                        </span>
                      </button>

                      <button
                        type="button"
                        className="hover:text-destructive absolute right-1.5 top-1.5 flex h-7 w-7 items-center justify-center rounded-md text-muted-foreground opacity-70 transition hover:bg-background focus:opacity-100 group-hover:opacity-100"
                        onClick={() => onDelete(conversation.id)}
                        title="Delete chat"
                      >
                        <MoreVertical size={14} aria-hidden />
                      </button>
                    </div>
                  );
                })}
              </section>
            ))}
            {hasMore ? (
              <Button
                className="mt-2 w-full"
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
            icon={<Search size={18} aria-hidden />}
            title={searchQuery.trim() ? "No matching chats" : "No chats yet"}
            description={
              searchQuery.trim()
                ? "Clear search or create a new conversation."
                : "Create a chat to start asking grounded questions."
            }
            action={
              <Button
                type="button"
                variant="secondary"
                onClick={() => onCreate(`New chat ${conversations.length + 1}`)}
                disabled={isCreating}
              >
                <Plus size={16} aria-hidden />
                New chat
              </Button>
            }
          />
        )}
      </div>

      <button
        type="button"
        className="mx-2 mb-2 flex items-center gap-2 rounded-lg border border-border bg-background/40 px-3 py-2 text-xs text-muted-foreground transition hover:bg-muted/40 hover:text-foreground"
        title="Archived chats"
      >
        <Archive size={14} aria-hidden />
        <span className="min-w-0 flex-1 text-left">Archived chats</span>
        <span className="rounded-full border border-border bg-card px-2 py-0.5 text-[11px]">
          {formatArchivedCount(0)}
        </span>
        <ChevronDown size={14} aria-hidden />
      </button>
    </aside>
  );
}
