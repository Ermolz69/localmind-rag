import { beforeEach, describe, expect, it, vi } from "vitest";

const { mockGetCompanionToken } = vi.hoisted(() => ({
  mockGetCompanionToken: vi.fn<() => string | null>(),
}));

vi.mock("@shared/lib/companionAuth", () => ({
  getCompanionToken: mockGetCompanionToken,
}));

import { request, setApiBaseUrl } from "./http";

function jsonResponse(data: unknown): Response {
  return {
    ok: true,
    status: 200,
    headers: {
      get: (key: string) =>
        key.toLowerCase() === "content-type" ? "application/json" : null,
    },
    json: async () => ({
      success: true,
      data,
      error: null,
      metadata: { requestId: "r" },
    }),
  } as unknown as Response;
}

describe("request authorization header", () => {
  let fetchMock: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    vi.clearAllMocks();
    setApiBaseUrl("http://gateway");
    fetchMock = vi.fn().mockResolvedValue(jsonResponse({ ok: true }));
    vi.stubGlobal("fetch", fetchMock);
  });

  it("attaches the companion bearer token when present", async () => {
    mockGetCompanionToken.mockReturnValue("dev-token");

    await request("/companion/info");

    const init = fetchMock.mock.calls[0]?.[1] as RequestInit;
    const headers = init.headers as Record<string, string>;
    expect(headers.Authorization).toBe("Bearer dev-token");
  });

  it("omits authorization when there is no token", async () => {
    mockGetCompanionToken.mockReturnValue(null);

    await request("/companion/info");

    const init = fetchMock.mock.calls[0]?.[1] as RequestInit;
    const headers = init.headers as Record<string, string>;
    expect(headers.Authorization).toBeUndefined();
  });
});
