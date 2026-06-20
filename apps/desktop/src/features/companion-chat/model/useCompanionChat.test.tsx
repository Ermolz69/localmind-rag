import { act, renderHook } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { useCompanionChat } from "./useCompanionChat";

const { mockCreateChat, mockStreamChatMessage } = vi.hoisted(() => ({
  mockCreateChat: vi.fn(),
  mockStreamChatMessage: vi.fn(),
}));

vi.mock("@shared/api", () => ({
  chatsApi: {
    createChat: mockCreateChat,
    streamChatMessage: mockStreamChatMessage,
  },
  getErrorMessage: (_error: unknown, fallback: string) => fallback,
}));

const source = {
  chunkId: "chunk-1",
  documentId: "doc-1",
  documentName: "Notes.pdf",
  pageNumber: null,
  score: 0.9,
  snippet: "excerpt",
};

describe("useCompanionChat", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockCreateChat.mockResolvedValue({ id: "conv-1" });
    mockStreamChatMessage.mockImplementation(async function* () {
      yield { text: "Hello", sources: [] };
      yield { text: " world", sources: [source] };
    });
  });

  it("creates a conversation and streams the answer with sources", async () => {
    const { result } = renderHook(() => useCompanionChat());

    await act(async () => {
      await result.current.send("What is this?");
    });

    expect(mockCreateChat).toHaveBeenCalledOnce();
    expect(result.current.messages).toHaveLength(2);

    const [userMessage, assistantMessage] = result.current.messages;
    expect(userMessage.role).toBe("user");
    expect(userMessage.content).toBe("What is this?");
    expect(assistantMessage.role).toBe("assistant");
    expect(assistantMessage.content).toBe("Hello world");
    expect(assistantMessage.status).toBe("ready");
    expect(assistantMessage.sources).toEqual([source]);
  });

  it("reuses the conversation for follow-up messages", async () => {
    const { result } = renderHook(() => useCompanionChat());

    await act(async () => {
      await result.current.send("First");
    });
    await act(async () => {
      await result.current.send("Second");
    });

    expect(mockCreateChat).toHaveBeenCalledOnce();
    expect(result.current.messages).toHaveLength(4);
  });

  it("does not send a blank message", async () => {
    const { result } = renderHook(() => useCompanionChat());

    await act(async () => {
      await result.current.send("   ");
    });

    expect(mockCreateChat).not.toHaveBeenCalled();
    expect(result.current.messages).toHaveLength(0);
  });

  it("marks the assistant message as errored on stream failure", async () => {
    mockStreamChatMessage.mockImplementationOnce(
      // eslint-disable-next-line require-yield
      async function* () {
        throw new Error("stream failed");
      },
    );
    const { result } = renderHook(() => useCompanionChat());

    await act(async () => {
      await result.current.send("Question");
    });

    expect(result.current.error).toBe("The answer could not be generated.");
    expect(result.current.messages[1].status).toBe("error");
  });
});
