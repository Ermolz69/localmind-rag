import { Bot, Check, Loader2, Menu, Pencil, X } from "lucide-react";
import { useEffect, useState, type RefObject } from "react";
import type { ChatConversation } from "@entities/chat";
import { cn } from "@shared/lib/cn";
import { Button, EmptyState, Input } from "@shared/ui";
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
};

export function ChatThread({
  conversation,
  messages,
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

  useEffect(() => {
    setDraftTitle(conversation?.title ?? "");
    setIsEditingTitle(false);
  }, [conversation?.id, conversation?.title]);

  return (
    <div className="flex min-h-0 flex-1 flex-col overflow-hidden">
      <div className="border-b border-border bg-card/30 px-6 py-4">
        <div className="flex items-center gap-4">
          <Button
            variant="ghost"
            className="h-9 w-9 flex-shrink-0 p-0 hover:bg-muted"
            onClick={onToggleSidebar}
            title={isSidebarOpen ? "Close chats" : "Open chats"}
          >
            <Menu size={20} aria-hidden />
          </Button>
          <div className="min-w-0 flex-1">
            {conversation ? (
              isEditingTitle ? (
                <form
                  className="flex max-w-xl items-center gap-2"
                  onSubmit={async (event) => {
                    event.preventDefault();
                    if (
                      draftTitle.trim() &&
                      draftTitle.trim() !== conversation.title
                    ) {
                      await onRename(draftTitle);
                    }
                    setIsEditingTitle(false);
                  }}
                >
                  <Input
                    className="h-9 max-w-sm px-3 text-sm font-semibold"
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
                    variant="primary"
                    className="h-9 shrink-0 px-3"
                    disabled={
                      isRenaming ||
                      !draftTitle.trim() ||
                      draftTitle.trim() === conversation.title
                    }
                  >
                    {isRenaming ? (
                      <Loader2 className="animate-spin" size={14} aria-hidden />
                    ) : (
                      <Check size={14} aria-hidden />
                    )}
                  </Button>
                  <Button
                    type="button"
                    variant="ghost"
                    className="h-9 shrink-0 px-3"
                    onClick={() => {
                      setDraftTitle(conversation.title);
                      setIsEditingTitle(false);
                    }}
                  >
                    <X size={14} aria-hidden />
                  </Button>
                </form>
              ) : (
                <div className="group/title flex items-center gap-2">
                  <h2 className="select-text truncate text-base font-semibold">
                    {conversation.title}
                  </h2>
                  <button
                    type="button"
                    className="rounded-md p-1 text-muted-foreground opacity-0 transition-opacity hover:text-foreground focus:opacity-100 group-hover/title:opacity-100"
                    onClick={() => setIsEditingTitle(true)}
                    title="Rename conversation"
                  >
                    <Pencil size={14} />
                  </button>
                </div>
              )
            ) : (
              <h2 className="text-base font-semibold">Conversation</h2>
            )}
            {!isEditingTitle && (
              <p className="mt-0.5 max-w-md truncate text-xs text-muted-foreground">
                {conversation
                  ? "Ask a question and the assistant will answer from local sources."
                  : "Create or select a conversation to begin."}
              </p>
            )}
          </div>
          <div className="flex-shrink-0 select-none rounded-full border border-border bg-background/50 px-2.5 py-0.5 text-[11px] font-medium tracking-wide text-muted-foreground">
            {messages.length > 0 ? `${messages.length} messages` : "Empty"}
          </div>
        </div>
      </div>

      <div className="min-h-0 flex-1 overflow-y-auto p-6 md:p-8">
        {isLoadingMessages ? (
          <div className="mx-auto w-full max-w-3xl rounded-md border border-border bg-background p-4 text-sm text-muted-foreground">
            Loading messages...
          </div>
        ) : messages.length > 0 ? (
          <div className="mx-auto w-full max-w-3xl space-y-6">
            {messages.map((message) =>
              message.role === "user" ? (
                <div className="flex w-full justify-end" key={message.id}>
                  <div className="max-w-[80%] select-text whitespace-pre-wrap rounded-2xl bg-muted px-4 py-2.5 text-sm leading-6 text-foreground shadow-sm">
                    {message.content}
                  </div>
                </div>
              ) : (
                <div
                  key={message.id}
                  className={cn(
                    "flex w-full select-text items-start gap-4 text-sm",
                    message.status === "error" && "text-destructive",
                  )}
                  onClick={() => {
                    if (message.sources.length > 0) {
                      onSelectSourceMessage(message.id);
                    }
                  }}
                >
                  <div className="flex h-8 w-8 shrink-0 select-none items-center justify-center rounded-full border border-border bg-background text-primary shadow-sm">
                    <Bot size={16} aria-hidden />
                  </div>
                  <div className="min-w-0 flex-1 space-y-2">
                    <div className="flex items-center gap-2 text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                      <span>Assistant</span>
                      {message.status === "pending" ? (
                        <span className="inline-flex animate-pulse items-center gap-1.5 rounded-full bg-primary/10 px-2 py-0.5 text-[10px] font-medium normal-case text-primary">
                          Thinking...
                        </span>
                      ) : null}
                    </div>
                    <div className="whitespace-pre-wrap leading-7 text-foreground">
                      {message.content}
                    </div>
                    {message.error ? (
                      <p className="mt-2 rounded-md border border-rose-500/20 bg-rose-500/10 p-2 text-xs font-medium text-rose-500">
                        {message.error}
                      </p>
                    ) : null}
                    {message.sources.length > 0 ? (
                      <div className="mt-3 flex flex-wrap gap-2">
                        {message.sources.map((source) => (
                          <button
                            key={source.chunkId}
                            type="button"
                            onClick={() => onSelectSourceMessage(message.id)}
                            className="inline-flex cursor-pointer items-center gap-1 rounded-md border border-border bg-muted/30 px-2.5 py-1 text-xs text-muted-foreground transition hover:bg-muted/70"
                          >
                            <span className="max-w-[200px] truncate">
                              {source.documentName}
                            </span>
                            {source.pageNumber ? (
                              <span className="font-mono text-muted-foreground/60">
                                · p.{source.pageNumber}
                              </span>
                            ) : null}
                          </button>
                        ))}
                      </div>
                    ) : null}
                  </div>
                </div>
              ),
            )}
            <div ref={threadEndRef} />
          </div>
        ) : (
          <div className="flex h-full flex-1 items-center justify-center p-6">
            <EmptyState
              icon={<Bot size={18} aria-hidden />}
              title={
                conversation
                  ? "Ask your first question"
                  : "Create a conversation"
              }
              description={
                conversation
                  ? "Questions to the local API will produce an answer and source snippets from your indexed documents."
                  : "Start a new chat, then ask a question to generate a grounded answer."
              }
              action={
                <Button type="button" variant="secondary" onClick={onCreate}>
                  New conversation
                </Button>
              }
            />
          </div>
        )}
      </div>
    </div>
  );
}
