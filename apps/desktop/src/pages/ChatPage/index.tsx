import { AlertCircle, Bot, Loader2, Plus, Send, Menu } from "lucide-react";
import { useEffect, useMemo, useRef, useState, type FormEvent } from "react";
import { localApi, type RagSource } from "../../shared/api/client";
import { cn } from "../../shared/lib/cn";
import { EmptyState } from "../../shared/ui/EmptyState";
import { Button } from "../../shared/ui/Button";
import { SourcePanel } from "../../widgets/SourcePanel/SourcePanel";

type MessageRole = "user" | "assistant";

type MessageStatus = "pending" | "ready" | "error";

type ChatMessage = {
  id: string;
  role: MessageRole;
  content: string;
  status: MessageStatus;
  sources: RagSource[];
  error?: string;
};

export function ChatPage() {
  const [conversations, setConversations] = useState<
    Array<{ id: string; title: string }>
  >([]);
  const [selectedConversationId, setSelectedConversationId] = useState<
    string | null
  >(null);
  const [messagesByConversation, setMessagesByConversation] = useState<
    Record<string, ChatMessage[]>
  >({});
  const [newConversationTitle, setNewConversationTitle] = useState("");
  const [question, setQuestion] = useState("");
  const [conversationError, setConversationError] = useState<string | null>(
    null,
  );
  const [isLoadingConversations, setIsLoadingConversations] = useState(true);
  const [isCreatingConversation, setIsCreatingConversation] = useState(false);
  const [isSendingQuestion, setIsSendingQuestion] = useState(false);
  const [activeSourceMessageId, setActiveSourceMessageId] = useState<
    string | null
  >(null);
  const [isSidebarOpen, setIsSidebarOpen] = useState(false);
  const threadEndRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    let isMounted = true;

    async function loadConversations() {
      setIsLoadingConversations(true);
      setConversationError(null);

      try {
        const chats = await localApi.getChats();
        if (!isMounted) {
          return;
        }

        setConversations(chats);
        setSelectedConversationId((current) => current ?? chats[0]?.id ?? null);
      } catch (exception) {
        if (!isMounted) {
          return;
        }

        setConversationError(
          exception instanceof Error
            ? exception.message
            : "Failed to load conversations.",
        );
      } finally {
        if (isMounted) {
          setIsLoadingConversations(false);
        }
      }
    }

    void loadConversations();

    return () => {
      isMounted = false;
    };
  }, []);

  const selectedConversation = useMemo(
    () =>
      conversations.find(
        (conversation) => conversation.id === selectedConversationId,
      ) ?? null,
    [conversations, selectedConversationId],
  );

  const selectedMessages = selectedConversationId
    ? (messagesByConversation[selectedConversationId] ?? [])
    : [];

  const selectedAssistantMessage = useMemo(() => {
    if (!selectedMessages.length) {
      return null;
    }

    const byId = selectedMessages.find(
      (message) =>
        message.id === activeSourceMessageId &&
        message.role === "assistant" &&
        message.sources.length > 0,
    );

    if (byId) {
      return byId;
    }

    return (
      [...selectedMessages]
        .reverse()
        .find(
          (message) =>
            message.role === "assistant" && message.sources.length > 0,
        ) ?? null
    );
  }, [activeSourceMessageId, selectedMessages]);

  const activeSources = selectedAssistantMessage?.sources ?? [];

  useEffect(() => {
    threadEndRef.current?.scrollIntoView({ behavior: "smooth", block: "end" });
  }, [selectedMessages, isSendingQuestion, selectedConversationId]);

  async function createConversation(title: string) {
    setConversationError(null);
    setIsCreatingConversation(true);

    try {
      const conversation = await localApi.createChat({ title });
      setConversations((current) => [conversation, ...current]);
      setSelectedConversationId(conversation.id);
      setMessagesByConversation((current) => ({
        ...current,
        [conversation.id]: current[conversation.id] ?? [],
      }));
      setActiveSourceMessageId(null);
      return conversation;
    } catch (exception) {
      setConversationError(
        exception instanceof Error
          ? exception.message
          : "Failed to create conversation.",
      );
      return null;
    } finally {
      setIsCreatingConversation(false);
    }
  }

  async function handleCreateConversation(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const title =
      newConversationTitle.trim() || `New chat ${conversations.length + 1}`;

    const conversation = await createConversation(title);
    if (conversation) {
      setNewConversationTitle("");
    }
  }

  async function handleSendQuestion(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const content = question.trim();
    if (!content || isSendingQuestion) {
      return;
    }

    setConversationError(null);
    setIsSendingQuestion(true);

    let conversationId = selectedConversationId;

    if (!conversationId) {
      const createdConversation = await createConversation(
        newConversationTitle.trim() || content.slice(0, 48) || "New chat",
      );

      if (!createdConversation) {
        setIsSendingQuestion(false);
        return;
      }

      conversationId = createdConversation.id;
    }

    setQuestion("");

    const userMessageId = crypto.randomUUID();
    const assistantMessageId = crypto.randomUUID();

    setMessagesByConversation((current) => {
      const messages = current[conversationId] ?? [];

      return {
        ...current,
        [conversationId]: [
          ...messages,
          {
            id: userMessageId,
            role: "user",
            content,
            status: "ready",
            sources: [],
          },
          {
            id: assistantMessageId,
            role: "assistant",
            content: "Thinking through your documents...",
            status: "pending",
            sources: [],
          },
        ],
      };
    });
    setSelectedConversationId(conversationId);
    setActiveSourceMessageId(assistantMessageId);

    try {
      const answer = await localApi.sendChatMessage(conversationId, content);
      setMessagesByConversation((current) => {
        const messages = current[conversationId] ?? [];

        return {
          ...current,
          [conversationId]: messages.map((message) =>
            message.id === assistantMessageId
              ? {
                  ...message,
                  content: answer.answer,
                  status: "ready",
                  sources: answer.sources,
                }
              : message,
          ),
        };
      });
      setActiveSourceMessageId(assistantMessageId);
    } catch (exception) {
      setMessagesByConversation((current) => {
        const messages = current[conversationId] ?? [];

        return {
          ...current,
          [conversationId]: messages.map((message) =>
            message.id === assistantMessageId
              ? {
                  ...message,
                  content: "I couldn't generate an answer for that question.",
                  status: "error",
                  sources: [],
                  error:
                    exception instanceof Error
                      ? exception.message
                      : "The local API request failed.",
                }
              : message,
          ),
        };
      });
      setConversationError(
        exception instanceof Error
          ? exception.message
          : "The local API request failed.",
      );
      setActiveSourceMessageId(null);
    } finally {
      setIsSendingQuestion(false);
    }
  }

  function selectConversation(conversationId: string) {
    setSelectedConversationId(conversationId);
    const lastAssistantWithSources = [
      ...(messagesByConversation[conversationId] ?? []),
    ]
      .reverse()
      .find(
        (message) => message.role === "assistant" && message.sources.length > 0,
      );
    setActiveSourceMessageId(lastAssistantWithSources?.id ?? null);
  }

  function renderEmptyThread() {
    return (
      <div className="flex flex-1 items-center justify-center p-6">
        <EmptyState
          icon={<Bot size={18} aria-hidden />}
          title={
            selectedConversation
              ? "Ask your first question"
              : "Create a conversation"
          }
          description={
            selectedConversation
              ? "Questions to the local API will produce an answer and source snippets from your indexed documents."
              : "Start a new chat, then ask a question to generate a grounded answer."
          }
          action={
            <Button
              type="button"
              variant="secondary"
              onClick={() => {
                void createConversation(`New chat ${conversations.length + 1}`);
              }}
              disabled={isCreatingConversation}
            >
              {isCreatingConversation ? (
                <Loader2 size={16} className="animate-spin" aria-hidden />
              ) : (
                <Plus size={16} aria-hidden />
              )}
              New conversation
            </Button>
          }
        />
      </div>
    );
  }

  return (
    <section
      className={cn(
        "grid h-[calc(100vh-6rem)] min-h-[500px] gap-4",
        isSidebarOpen
          ? "grid-cols-[220px_1fr_260px] lg:grid-cols-[240px_1fr_300px] xl:grid-cols-[260px_1fr_320px]"
          : "grid-cols-[1fr_260px] lg:grid-cols-[1fr_300px] xl:grid-cols-[1fr_320px]",
      )}
    >
      {isSidebarOpen && (
        <aside className="flex min-h-0 flex-col overflow-hidden rounded-md border border-border bg-card">
          <div className="border-b border-border p-4">
            <div className="flex items-center gap-2">
              <div className="flex h-9 w-9 items-center justify-center rounded-md bg-primary/10 text-primary">
                <Bot size={18} aria-hidden />
              </div>
              <div>
                <h1 className="text-lg font-semibold">Local RAG chat</h1>
                <p className="text-sm text-muted-foreground">
                  Conversations backed by the local API.
                </p>
              </div>
            </div>

            <form
              className="mt-4 flex gap-2"
              onSubmit={handleCreateConversation}
            >
              <input
                className="h-10 min-w-0 flex-1 rounded-md border border-border bg-background px-3 text-sm outline-none placeholder:text-muted-foreground"
                value={newConversationTitle}
                onChange={(event) =>
                  setNewConversationTitle(event.target.value)
                }
                placeholder="New conversation title"
              />
              <Button
                type="submit"
                variant="secondary"
                disabled={isCreatingConversation}
              >
                {isCreatingConversation ? (
                  <Loader2 size={16} className="animate-spin" aria-hidden />
                ) : (
                  <Plus size={16} aria-hidden />
                )}
                Create
              </Button>
            </form>
          </div>

          <div className="min-h-0 flex-1 overflow-y-auto p-3">
            {isLoadingConversations ? (
              <div className="rounded-md border border-dashed border-border p-4 text-sm text-muted-foreground">
                Loading conversations...
              </div>
            ) : conversations.length > 0 ? (
              <div className="space-y-2">
                {conversations.map((conversation) => {
                  const messageCount =
                    messagesByConversation[conversation.id]?.length ?? 0;
                  const isActive = conversation.id === selectedConversationId;

                  return (
                    <button
                      key={conversation.id}
                      type="button"
                      onClick={() => selectConversation(conversation.id)}
                      className={cn(
                        "w-full rounded-md border p-3 text-left transition",
                        isActive
                          ? "border-primary/40 bg-primary/10"
                          : "border-border bg-background hover:bg-muted/50",
                      )}
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
                  );
                })}
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
                    onClick={() => {
                      void createConversation(
                        `New chat ${conversations.length + 1}`,
                      );
                    }}
                    disabled={isCreatingConversation}
                  >
                    {isCreatingConversation ? (
                      <Loader2 size={16} className="animate-spin" aria-hidden />
                    ) : (
                      <Plus size={16} aria-hidden />
                    )}
                    New conversation
                  </Button>
                }
              />
            )}
          </div>
        </aside>
      )}

      <div className="flex min-h-0 flex-col overflow-hidden rounded-md border border-border bg-card">
        <div className="flex items-center gap-3 border-b border-border p-4">
          <Button
            variant="secondary"
            className="flex-shrink-0 px-2 py-2"
            onClick={() => setIsSidebarOpen((prev) => !prev)}
            title={isSidebarOpen ? "Close chats" : "Open chats"}
          >
            <Menu size={18} />
          </Button>
          <div className="flex flex-1 items-start justify-between gap-4">
            <div>
              <h2 className="text-lg font-semibold">
                {selectedConversation?.title ?? "Conversation"}
              </h2>
              <p className="text-sm text-muted-foreground">
                {selectedConversation
                  ? "Ask a question and the assistant will answer from local sources."
                  : "Create or select a conversation to begin."}
              </p>
            </div>
            <div className="rounded-full border border-border bg-background px-3 py-1 text-xs text-muted-foreground">
              {selectedMessages.length > 0
                ? `${selectedMessages.length} messages`
                : "Empty"}
            </div>
          </div>

          {conversationError ? (
            <div className="border-destructive/20 bg-destructive/10 text-destructive mt-3 flex items-start gap-2 rounded-md border p-3 text-sm">
              <AlertCircle size={16} className="mt-0.5 shrink-0" aria-hidden />
              <p>{conversationError}</p>
            </div>
          ) : null}
        </div>

        <div className="min-h-0 flex-1 overflow-y-auto p-4">
          {selectedMessages.length > 0 ? (
            <div className="space-y-4">
              {selectedMessages.map((message) => (
                <article
                  key={message.id}
                  className={cn(
                    "max-w-[min(56rem,92%)] rounded-2xl border px-4 py-3 text-sm shadow-sm",
                    message.role === "user"
                      ? "ml-auto border-primary/20 bg-primary/10"
                      : message.status === "error"
                        ? "border-destructive/20 bg-destructive/10"
                        : "border-border bg-background",
                  )}
                  onClick={() => {
                    if (
                      message.role === "assistant" &&
                      message.sources.length > 0
                    ) {
                      setActiveSourceMessageId(message.id);
                    }
                  }}
                >
                  <div className="mb-2 flex items-center gap-2 text-xs font-medium uppercase tracking-wide text-muted-foreground">
                    {message.role === "assistant" ? (
                      <Bot size={13} aria-hidden />
                    ) : null}
                    <span>
                      {message.role === "assistant" ? "Assistant" : "You"}
                    </span>
                    {message.status === "pending" ? (
                      <span className="rounded-full bg-muted px-2 py-0.5 text-[10px] normal-case tracking-normal text-muted-foreground">
                        Loading answer
                      </span>
                    ) : null}
                  </div>

                  <p className="whitespace-pre-wrap leading-6 text-foreground">
                    {message.content}
                  </p>

                  {message.status === "error" && message.error ? (
                    <p className="text-destructive mt-2 text-xs">
                      {message.error}
                    </p>
                  ) : null}

                  {message.role === "assistant" &&
                  message.sources.length > 0 ? (
                    <div className="mt-3 flex flex-wrap gap-2">
                      {message.sources.map((source) => (
                        <button
                          key={source.chunkId}
                          type="button"
                          onClick={() => setActiveSourceMessageId(message.id)}
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
            renderEmptyThread()
          )}
        </div>

        <form
          className="border-t border-border p-3"
          onSubmit={handleSendQuestion}
        >
          <div className="flex gap-2">
            <textarea
              className="min-h-[56px] flex-1 resize-none rounded-md border border-border bg-background px-3 py-2 text-sm outline-none placeholder:text-muted-foreground"
              value={question}
              onChange={(event) => setQuestion(event.target.value)}
              placeholder="Ask your documents"
              rows={2}
              disabled={isSendingQuestion || isCreatingConversation}
            />
            <Button
              type="submit"
              className="self-end"
              disabled={
                !question.trim() || isSendingQuestion || isCreatingConversation
              }
            >
              {isSendingQuestion ? (
                <Loader2 size={16} className="animate-spin" aria-hidden />
              ) : (
                <Send size={16} aria-hidden />
              )}
              Ask
            </Button>
          </div>
        </form>
      </div>

      <aside className="flex min-h-0 flex-col overflow-hidden rounded-md border border-border bg-card">
        <div className="border-b border-border p-4">
          <h2 className="text-sm font-semibold">Sources</h2>
          <p className="mt-1 text-sm text-muted-foreground">
            Snippets from the answer context appear here.
          </p>
        </div>

        <div className="min-h-0 flex-1 overflow-y-auto p-4">
          {activeSources.length > 0 ? (
            <SourcePanel sources={activeSources} />
          ) : (
            <EmptyState
              icon={<Bot size={18} aria-hidden />}
              title={
                selectedAssistantMessage
                  ? "No sources available"
                  : "No answer selected"
              }
              description={
                selectedAssistantMessage
                  ? "This assistant message did not return any grounded snippets."
                  : "When the assistant answers, the strongest snippets will be shown here."
              }
            />
          )}
        </div>
      </aside>
    </section>
  );
}
