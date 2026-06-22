import { useEffect, useRef, useState } from "react";
import { Send } from "lucide-react";

import type { RagSource } from "@entities/source";
import { Button, Input } from "@shared/ui";
import { cn } from "@shared/lib/cn";

import {
  useCompanionChat,
  type CompanionChatMessage,
} from "../model/useCompanionChat";

function ChatSources({ sources }: { sources: RagSource[] }) {
  if (sources.length === 0) {
    return null;
  }

  return (
    <details className="mt-2 rounded-lg border border-border bg-background/60 p-2">
      <summary className="cursor-pointer text-xs font-medium text-muted-foreground">
        Sources ({sources.length})
      </summary>
      <ul className="mt-2 space-y-2">
        {sources.map((source) => (
          <li key={source.chunkId} className="text-xs">
            <p className="font-medium text-foreground">{source.documentName}</p>
            <p className="mt-0.5 text-muted-foreground">{source.snippet}</p>
          </li>
        ))}
      </ul>
    </details>
  );
}

function ChatBubble({ message }: { message: CompanionChatMessage }) {
  const isUser = message.role === "user";

  return (
    <div className={cn("flex", isUser ? "justify-end" : "justify-start")}>
      <div
        className={cn(
          "max-w-[85%] rounded-2xl px-3 py-2 text-sm",
          isUser
            ? "bg-primary text-primary-foreground"
            : "bg-card text-card-foreground",
          message.status === "error" && "border-destructive border",
        )}
      >
        <p className="whitespace-pre-wrap">
          {message.content || (message.status === "pending" ? "Thinking…" : "")}
        </p>
        {!isUser ? <ChatSources sources={message.sources} /> : null}
      </div>
    </div>
  );
}

export function CompanionChat() {
  const { messages, isStreaming, error, send } = useCompanionChat();
  const [draft, setDraft] = useState("");
  const listEndRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    listEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages]);

  function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    const content = draft;
    setDraft("");
    void send(content);
  }

  return (
    <div className="flex min-h-0 flex-1 flex-col gap-3">
      <div className="flex min-h-0 flex-1 flex-col gap-3 overflow-y-auto">
        {messages.length === 0 ? (
          <p className="text-sm text-muted-foreground">
            Ask anything about your knowledge base on the computer.
          </p>
        ) : (
          messages.map((message) => (
            <ChatBubble key={message.id} message={message} />
          ))
        )}
        <div ref={listEndRef} />
      </div>

      {error ? <p className="text-destructive text-sm">{error}</p> : null}

      <form onSubmit={handleSubmit} className="flex gap-2">
        <Input
          value={draft}
          onChange={(event) => setDraft(event.target.value)}
          placeholder="Ask a question"
          aria-label="Message"
          className="flex-1"
        />
        <Button type="submit" disabled={isStreaming || !draft.trim()}>
          <Send className="h-4 w-4" />
        </Button>
      </form>
    </div>
  );
}
