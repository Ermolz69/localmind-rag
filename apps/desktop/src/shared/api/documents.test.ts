import { afterEach, describe, expect, it, vi } from "vitest";
import { documentsApi } from "./documents";
import { setApiBaseUrl } from "./http";

const apiBaseUrl = "http://127.0.0.1:49321";

function successfulEmptyResponse() {
  return new Response(
    JSON.stringify({
      success: true,
      data: null,
      error: null,
      metadata: {
        timestamp: "2026-06-21T12:00:00Z",
        requestId: "request-id",
      },
    }),
    {
      status: 200,
      headers: { "Content-Type": "application/json" },
    },
  );
}

describe("documentsApi.deleteDocument", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("issues a DELETE to the document resource", async () => {
    setApiBaseUrl(apiBaseUrl);
    const fetchMock = vi
      .fn<typeof fetch>()
      .mockResolvedValue(successfulEmptyResponse());
    vi.stubGlobal("fetch", fetchMock);

    await documentsApi.deleteDocument("doc-123");

    expect(fetchMock).toHaveBeenCalledTimes(1);
    const [url, init] = fetchMock.mock.calls[0];
    expect(url).toBe(`${apiBaseUrl}/api/v1/documents/doc-123`);
    expect(init?.method).toBe("DELETE");
  });
});
