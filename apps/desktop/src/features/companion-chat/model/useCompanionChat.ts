import { useCallback, useEffect, useRef, useState } from "react";

import type { RagSource } from "@entities/source";
import { chatsApi, getErrorMessage } from "@shared/api";

export type CompanionChatMessage = {
  id: string;
  role: "user" | "assistant";
  content: string;
  status: "pending" | "ready" | "error";
  sources: RagSource[];
};

function isAbortError(error: unknown): boolean {
  return error instanceof DOMException && error.name === "AbortError";
}

/**
 * Drives a single mobile RAG conversation against the computer's local knowledge
 * base: lazily creates the conversation, streams the answer, and collects
 * sources. Read-only over already-indexed documents.
 */
export function useCompanionChat() {
  const [messages, setMessages] = useState<CompanionChatMessage[]>([]);
  const [isStreaming, setIsStreaming] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const conversationIdRef = useRef<string | null>(null);
  const abortRef = useRef<AbortController | null>(null);

  useEffect(() => () => abortRef.current?.abort(), []);

  const updateAssistant = useCallback(
    (id: string, patch: Partial<CompanionChatMessage>) => {
      setMessages((previous) =>
        previous.map((message) =>
          message.id === id ? { ...message, ...patch } : message,
        ),
      );
    },
    [],
  );

  const send = useCallback(
    async (rawContent: string) => {
      const content = rawContent.trim();

      if (!content || isStreaming) {
        return;
      }

      setError(null);

      let conversationId = conversationIdRef.current;

      if (!conversationId) {
        try {
          const created = await chatsApi.createChat({
            title: "Companion chat",
          });
          conversationId = created.id;
          conversationIdRef.current = conversationId;
        } catch (exception) {
          setError(getErrorMessage(exception, "Unable to start the chat."));
          return;
        }
      }

      const userId = crypto.randomUUID();
      const assistantId = crypto.randomUUID();

      setMessages((previous) => [
        ...previous,
        { id: userId, role: "user", content, status: "ready", sources: [] },
        {
          id: assistantId,
          role: "assistant",
          content: "",
          status: "pending",
          sources: [],
        },
      ]);

      const abortController = new AbortController();
      abortRef.current = abortController;
      setIsStreaming(true);

      let streamedText = "";
      let sources: RagSource[] = [];

      try {
        for await (const chunk of chatsApi.streamChatMessage(
          conversationId,
          content,
          undefined,
          abortController.signal,
        )) {
          streamedText += chunk.text ?? "";
          if (chunk.sources?.length) {
            sources = chunk.sources;
          }

          updateAssistant(assistantId, {
            content: streamedText,
            sources,
          });
        }

        updateAssistant(assistantId, {
          content: streamedText || "No answer was returned.",
          sources,
          status: "ready",
        });
      } catch (exception) {
        if (isAbortError(exception)) {
          updateAssistant(assistantId, {
            content: streamedText || "Generation cancelled.",
            status: "ready",
          });
          return;
        }

        const message = getErrorMessage(
          exception,
          "The answer could not be generated.",
        );
        setError(message);
        updateAssistant(assistantId, {
          content: streamedText || "Something went wrong.",
          status: "error",
        });
      } finally {
        if (abortRef.current === abortController) {
          abortRef.current = null;
        }
        setIsStreaming(false);
      }
    },
    [isStreaming, updateAssistant],
  );

  return { messages, isStreaming, error, send };
}
