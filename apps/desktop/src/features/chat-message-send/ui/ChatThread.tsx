import { Bot, Edit3, Loader2, Menu } from "lucide-react";
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

  useEffect(() => {
    setDraftTitle(conversation?.title ?? "");
  }, [conversation?.title]);

  return (
    <div className="flex min-h-0 flex-col overflow-hidden rounded-md border border-border bg-card">
      <div className="border-b border-border p-4">
        <div className="flex items-center gap-3">
          <Button
            variant="secondary"
            className="flex-shrink-0 px-2"
            onClick={onToggleSidebar}
            title={isSidebarOpen ? "Close chats" : "Open chats"}
          >
            <Menu size={18} aria-hidden />
          </Button>
          <div className="min-w-0 flex-1">
            {conversation ? (
              <form
                className="flex gap-2"
                onSubmit={(event) => {
                  event.preventDefault();
                  onRename(draftTitle);
                }}
              >
                <Input
                  className="max-w-md text-base font-semibold"
                  value={draftTitle}
                  onChange={(event) => setDraftTitle(event.target.value)}
                />
                <Button
                  type="submit"
                  variant="secondary"
                  disabled={
                    isRenaming || draftTitle.trim() === conversation.title
                  }
                >
                  {isRenaming ? (
                    <Loader2 className="animate-spin" size={16} aria-hidden />
                  ) : (
                    <Edit3 size={16} aria-hidden />
                  )}
                  Rename
                </Button>
              </form>
            ) : (
              <h2 className="text-lg font-semibold">Conversation</h2>
            )}
            <p className="mt-1 text-sm text-muted-foreground">
              {conversation
                ? "Ask a question and the assistant will answer from local sources."
                : "Create or select a conversation to begin."}
            </p>
          </div>
          <div className="rounded-full border border-border bg-background px-3 py-1 text-xs text-muted-foreground">
            {messages.length > 0 ? `${messages.length} messages` : "Empty"}
          </div>
        </div>
      </div>

      <div className="min-h-0 flex-1 overflow-y-auto p-4">
        {isLoadingMessages ? (
          <div className="rounded-md border border-border bg-background p-4 text-sm text-muted-foreground">
            Loading messages...
          </div>
        ) : messages.length > 0 ? (
          <div className="space-y-4">
            {messages.map((message) => (
              <article
                key={message.id}
                className={cn(
                  "max-w-[min(56rem,92%)] rounded-md border px-4 py-3 text-sm shadow-sm",
                  message.role === "user"
                    ? "ml-auto border-primary/20 bg-primary/10"
                    : message.status === "error"
                      ? "border-border bg-muted"
                      : "border-border bg-background",
                )}
                onClick={() => {
                  if (
                    message.role === "assistant" &&
                    message.sources.length > 0
                  ) {
                    onSelectSourceMessage(message.id);
                  }
                }}
              >
                <div className="mb-2 flex items-center gap-2 text-xs font-medium uppercase text-muted-foreground">
                  {message.role === "assistant" ? (
                    <Bot size={13} aria-hidden />
                  ) : null}
                  <span>
                    {message.role === "assistant" ? "Assistant" : "You"}
                  </span>
                  {message.status === "pending" ? (
                    <span className="rounded-full bg-muted px-2 py-0.5 text-[10px] normal-case text-muted-foreground">
                      Loading answer
                    </span>
                  ) : null}
                </div>
                <p className="whitespace-pre-wrap leading-6 text-foreground">
                  {message.content}
                </p>
                {message.error ? (
                  <p className="mt-2 text-xs text-muted-foreground">
                    {message.error}
                  </p>
                ) : null}
                {message.role === "assistant" && message.sources.length > 0 ? (
                  <div className="mt-3 flex flex-wrap gap-2">
                    {message.sources.map((source) => (
                      <button
                        key={source.chunkId}
                        type="button"
                        onClick={() => onSelectSourceMessage(message.id)}
                        className="rounded-full border border-border bg-muted/50 px-3 py-1 text-xs text-muted-foreground transition hover:bg-muted"
                      >
                        {source.documentName}
                        {source.pageNumber ? ` · p.${source.pageNumber}` : ""}
                      </button>
                    ))}
                  </div>
                ) : null}
              </article>
            ))}
            <div ref={threadEndRef} />
          </div>
        ) : (
          <div className="flex flex-1 items-center justify-center p-6">
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
