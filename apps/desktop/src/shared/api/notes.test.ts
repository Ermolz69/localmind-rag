import { afterEach, describe, expect, it, vi } from "vitest";
import { notesApi } from "./notes";
import { setApiBaseUrl } from "./http";

const apiBaseUrl = "http://127.0.0.1:49321";

function successfulEmptyResponse() {
  return new Response(
    JSON.stringify({
      success: true,
      data: null,
      error: null,
      metadata: {
        timestamp: "2026-06-18T12:00:00Z",
        requestId: "request-id",
      },
    }),
    {
      status: 200,
      headers: { "Content-Type": "application/json" },
    },
  );
}

describe("notesApi commands", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("returns an explicit success value after updating a note", async () => {
    setApiBaseUrl(apiBaseUrl);
    const fetchMock = vi
      .fn<typeof fetch>()
      .mockResolvedValue(successfulEmptyResponse());
    vi.stubGlobal("fetch", fetchMock);

    await expect(
      notesApi.updateNote("note-id", {
        title: "Renamed note",
        markdown: "Saved markdown",
        folderId: null,
      }),
    ).resolves.toBe(true);
  });
});
