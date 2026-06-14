import { afterEach, describe, expect, it, vi } from "vitest";
import { chatsApi } from "./chats";
import { setApiBaseUrl } from "./http";

const apiBaseUrl = "http://127.0.0.1:49321";

function successfulEmptyResponse() {
  return new Response(
    JSON.stringify({
      success: true,
      data: null,
      error: null,
      metadata: {
        timestamp: "2026-06-14T12:00:00Z",
        requestId: "request-id",
      },
    }),
    {
      status: 200,
      headers: { "Content-Type": "application/json" },
    },
  );
}

describe("chatsApi commands", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("returns an explicit success value after updating a chat", async () => {
    setApiBaseUrl(apiBaseUrl);
    const fetchMock = vi
      .fn<typeof fetch>()
      .mockResolvedValue(successfulEmptyResponse());
    vi.stubGlobal("fetch", fetchMock);

    await expect(
      chatsApi.updateChat("conversation-id", { title: "Renamed chat" }),
    ).resolves.toBe(true);
  });

  it("returns an explicit success value after deleting a chat", async () => {
    setApiBaseUrl(apiBaseUrl);
    const fetchMock = vi
      .fn<typeof fetch>()
      .mockResolvedValue(successfulEmptyResponse());
    vi.stubGlobal("fetch", fetchMock);

    await expect(chatsApi.deleteChat("conversation-id")).resolves.toBe(true);
  });
});
