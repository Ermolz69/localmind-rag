import {
  Bot,
  Check,
  FileText,
  Loader2,
  Menu,
  Pencil,
  Plus,
  X,
} from "lucide-react";
import { useEffect, useState, type RefObject } from "react";
import type { ChatConversation } from "@entities/chat";
import { cn } from "@shared/lib/cn";
import { Button, Input } from "@shared/ui";
import type { ChatMessageView } from "../model";

type ChatThreadProps = {
  conversation: ChatConversation | null;
  messages: ChatMessageView[];
  activeSourceMessageId: string | null;
  isLoadingMessages: boolean;
  isSidebarOpen: boolean;
  isRenaming: boolean;
  threadEndRef: RefObject<HTMLDivElement | null>;
  onCreate: () => void;
  onRename: (title: string) => void;
  onToggleSidebar: () => void;
  onSelectSourceMessage: (messageId: string) => void;
  onNewQuestion: () => void;
  onUsePrompt: (prompt: string) => void;
};

function formatUpdatedAt(conversation: ChatConversation | null) {
  if (!conversation) {
    return "No conversation selected";
  }

  return new Intl.DateTimeFormat(undefined, {
    hour: "2-digit",
    minute: "2-digit",
  }).format(new Date(conversation.updatedAt ?? conversation.createdAt));
}

function formatHeaderSourceCount(count: number) {
  return `${count} source${count === 1 ? "" : "s"}`;
}

function statusLabel(message: ChatMessageView) {
  if (message.status === "pending") {
    return "Thinking";
  }

  if (message.status === "error") {
    return "Needs attention";
  }

  return "Grounded";
}

export function ChatThread({
  conversation,
  messages,
  activeSourceMessageId,
  isLoadingMessages,
  isSidebarOpen,
  isRenaming,
  threadEndRef,
  onCreate,
  onRename,
  onToggleSidebar,
  onSelectSourceMessage,
}: ChatThreadProps) {
  const [draftTitle, setDraftTitle] = useState(conversation?.title ?? "");
  const [isEditingTitle, setIsEditingTitle] = useState(false);
  const assistantCount = messages.filter(
    (message) => message.role === "assistant",
  ).length;
  const userCount = messages.length - assistantCount;
  const sourceCount = messages.reduce(
    (total, message) => total + message.sources.length,
    0,
  );

  useEffect(() => {
    setDraftTitle(conversation?.title ?? "");
    setIsEditingTitle(false);
  }, [conversation?.id, conversation?.title]);

  return (
    <div className="flex min-h-0 flex-1 flex-col overflow-hidden">
      <header className="border-b border-border bg-gradient-to-r from-card via-muted/20 to-card px-3 py-2.5">
        <div className="flex min-w-0 items-center gap-3">
          <Button
            variant="ghost"
            className="h-8 w-8 flex-shrink-0 rounded-lg p-0"
            onClick={onToggleSidebar}
            title={isSidebarOpen ? "Close chats" : "Open chats"}
          >
            <Menu size={16} aria-hidden />
          </Button>

          <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg border border-primary/30 bg-primary/10 text-primary shadow-sm ring-1 ring-primary/10">
            <FileText size={16} aria-hidden />
          </div>

          <div className="min-w-0 flex-1">
            {conversation && isEditingTitle ? (
              <form
                className="flex max-w-xl items-center gap-2"
                onSubmit={async (event) => {
                  event.preventDefault();
                  if (
                    draftTitle.trim() &&
                    draftTitle.trim() !== conversation.title
                  ) {
                    await onRename(draftTitle.trim());
                  }
                  setIsEditingTitle(false);
                }}
              >
                <Input
                  className="h-8 max-w-md px-3 text-sm font-semibold"
                  value={draftTitle}
                  onChange={(event) => setDraftTitle(event.target.value)}
                  onKeyDown={(event) => {
                    if (event.key === "Escape") {
                      setDraftTitle(conversation.title);
                      setIsEditingTitle(false);
                    }
                  }}
                  autoFocus
                />
                <Button
                  type="submit"
                  className="h-8 w-8 shrink-0 p-0"
                  disabled={
                    isRenaming ||
                    !draftTitle.trim() ||
                    draftTitle.trim() === conversation.title
                  }
                  title="Save title"
                >
                  {isRenaming ? (
                    <Loader2 className="animate-spin" size={15} aria-hidden />
                  ) : (
                    <Check size={15} aria-hidden />
                  )}
                </Button>
                <Button
                  type="button"
                  variant="ghost"
                  className="h-8 w-8 shrink-0 p-0"
                  onClick={() => {
                    setDraftTitle(conversation.title);
                    setIsEditingTitle(false);
                  }}
                  title="Cancel rename"
                >
                  <X size={15} aria-hidden />
                </Button>
              </form>
            ) : (
              <div className="group/title flex min-w-0 items-center gap-2">
                <div className="min-w-0">
                  <div className="flex min-w-0 items-center gap-2">
                    <h2 className="truncate text-sm font-semibold">
                      {conversation?.title ?? "AI knowledge chat"}
                    </h2>
                  </div>
                  <p className="mt-0.5 flex min-w-0 items-center gap-2 truncate text-[11px] text-muted-foreground">
                    <span className="truncate">
                      {conversation
                        ? formatHeaderSourceCount(sourceCount)
                        : "No active sources"}
                    </span>
                    <span
                      className="h-1.5 w-1.5 shrink-0 rounded-full bg-primary"
                      aria-hidden
                    />
                    <span className="truncate">
                      {conversation
                        ? `Updated ${formatUpdatedAt(conversation)}`
                        : "Select or create a chat"}
                    </span>
                  </p>
                </div>
                {conversation ? (
                  <button
                    type="button"
                    className="rounded-md p-1 text-muted-foreground opacity-70 transition hover:bg-muted hover:text-foreground focus:opacity-100 group-hover/title:opacity-100"
                    onClick={() => setIsEditingTitle(true)}
                    title="Rename conversation"
                  >
                    <Pencil size={14} aria-hidden />
                  </button>
                ) : null}
              </div>
            )}
          </div>

          <div className="hidden shrink-0 items-center gap-2 sm:flex">
            <span className="inline-flex h-8 items-center gap-1.5 rounded-lg border border-border bg-background/80 px-3 text-xs font-medium text-muted-foreground shadow-sm">
              <FileText size={13} aria-hidden />
              {userCount} asked
            </span>
            <Button
              type="button"
              variant="secondary"
              className="h-8 rounded-lg px-3 text-xs"
              onClick={onCreate}
            >
              <Plus size={14} aria-hidden />
              New chat
            </Button>
          </div>
        </div>
      </header>

      <div className="min-h-0 flex-1 overflow-y-auto bg-gradient-to-b from-background/70 via-background to-card/40 px-5 py-6 pb-40">
        {isLoadingMessages ? (
          <div className="mx-auto w-full max-w-4xl space-y-4">
            <div className="h-20 animate-pulse rounded-xl bg-muted" />
            <div className="h-32 animate-pulse rounded-xl bg-muted" />
            <div className="ml-auto h-16 w-2/3 animate-pulse rounded-xl bg-muted" />
          </div>
        ) : messages.length > 0 ? (
          <div className="mx-auto flex w-full max-w-4xl flex-col gap-5">
            {messages.map((message) =>
              message.role === "user" ? (
                <div className="flex w-full justify-end" key={message.id}>
                  <article className="max-w-[78%] rounded-2xl rounded-br-md border border-primary/20 bg-primary px-4 py-3 text-sm leading-6 text-primary-foreground shadow-lg">
                    <p className="select-text whitespace-pre-wrap">
                      {message.content}
                    </p>
                  </article>
                </div>
              ) : (
                <article
                  key={message.id}
                  className={cn(
                    "group/message rounded-2xl rounded-tl-md border bg-card/95 p-4 shadow-lg backdrop-blur transition",
                    message.id === activeSourceMessageId
                      ? "border-primary/60 ring-1 ring-primary/20"
                      : "border-border hover:border-primary/30",
                    message.status === "error" && "border-destructive/40",
                    message.sources.length > 0 && "cursor-pointer",
                  )}
                  onClick={() => {
                    if (message.sources.length > 0) {
                      onSelectSourceMessage(message.id);
                    }
                  }}
                >
                  <div className="flex items-start gap-3">
                    <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl border border-primary/20 bg-primary/10 text-primary">
                      <Bot size={17} aria-hidden />
                    </div>
                    <div className="min-w-0 flex-1">
                      <div className="flex flex-wrap items-center gap-2">
                        <span className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                          LocalMind
                        </span>
                        <span
                          className={cn(
                            "rounded-full border border-border bg-muted px-2 py-0.5 text-[11px] font-medium text-muted-foreground",
                            message.status === "pending" &&
                              "animate-pulse border-primary/30 bg-primary/10 text-primary",
                            message.status === "error" &&
                              "border-destructive/30 bg-destructive/10 text-destructive",
                          )}
                        >
                          {statusLabel(message)}
                        </span>
                      </div>

                      <div className="mt-3 select-text whitespace-pre-wrap text-sm leading-7 text-foreground">
                        {message.content}
                      </div>

                      {message.error ? (
                        <p className="border-destructive/30 bg-destructive/10 text-destructive mt-3 rounded-md border p-2 text-xs font-medium">
                          {message.error}
                        </p>
                      ) : null}

                      {message.sources.length > 0 ? (
                        <div className="mt-4 flex flex-wrap gap-2">
                          {message.sources.map((source) => (
                            <button
                              key={source.chunkId}
                              type="button"
                              onClick={(event) => {
                                event.stopPropagation();
                                onSelectSourceMessage(message.id);
                              }}
                              className="inline-flex max-w-full items-center gap-1.5 rounded-lg border border-border bg-background/80 px-2.5 py-1 text-xs text-muted-foreground transition hover:border-primary/40 hover:text-foreground"
                              title={source.documentName}
                            >
                              <FileText size={13} aria-hidden />
                              <span className="max-w-[220px] truncate">
                                {source.documentName}
                              </span>
                              {source.pageNumber ? (
                                <span className="shrink-0 font-mono text-muted-foreground">
                                  p.{source.pageNumber}
                                </span>
                              ) : null}
                            </button>
                          ))}
                        </div>
                      ) : null}
                    </div>
                  </div>
                </article>
              ),
            )}
            <div ref={threadEndRef} />
          </div>
        ) : (
          <div className="mx-auto flex h-full max-w-4xl items-center justify-center px-4">
            <div className="w-full max-w-xl rounded-2xl border border-dashed border-border bg-card/60 p-8 text-center shadow-2xl backdrop-blur">
              <div className="mx-auto flex h-16 w-16 items-center justify-center rounded-2xl border border-primary/30 bg-primary/10 text-primary shadow-lg ring-1 ring-primary/20">
                <div className="flex h-11 w-11 items-center justify-center rounded-xl border border-primary/30 bg-primary/10">
                  <Bot size={22} aria-hidden />
                </div>
              </div>
              <h3 className="mt-6 text-lg font-semibold">
                {conversation
                  ? "Ask your first question"
                  : "Create a conversation"}
              </h3>
              <p className="mx-auto mt-2 max-w-sm text-sm leading-6 text-muted-foreground">
                {conversation
                  ? "Your answer will appear here with source snippets on the right."
                  : "Start a local knowledge chat, then ask from your documents."}
              </p>
              <Button
                type="button"
                variant="secondary"
                className="mt-5 rounded-lg border-primary/20 bg-primary/10 text-primary hover:bg-primary/20"
                onClick={onCreate}
              >
                <Plus size={16} aria-hidden />
                New conversation
              </Button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
