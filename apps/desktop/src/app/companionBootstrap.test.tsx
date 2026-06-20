import { beforeEach, describe, expect, it, vi } from "vitest";

import {
  clearCompanionToken,
  getCompanionToken,
  setCompanionToken,
} from "@shared/lib/companionAuth";

const { mockConfirmPairing, mockGetInfo, mockSetApiBaseUrl, FakeApiError } =
  vi.hoisted(() => {
    class FakeApiError extends Error {
      status: number;
      constructor(status: number) {
        super(`status ${status}`);
        this.name = "ApiError";
        this.status = status;
      }
    }
    return {
      mockConfirmPairing: vi.fn(),
      mockGetInfo: vi.fn(),
      mockSetApiBaseUrl: vi.fn(),
      FakeApiError,
    };
  });

vi.mock("@shared/api", () => ({
  setApiBaseUrl: mockSetApiBaseUrl,
  getErrorMessage: (_error: unknown, fallback: string) => fallback,
  ApiError: FakeApiError,
  companionApi: { confirmPairing: mockConfirmPairing, getInfo: mockGetInfo },
}));

import { bootstrapCompanionSession } from "./companionBootstrap";

describe("bootstrapCompanionSession", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    clearCompanionToken();
    mockConfirmPairing.mockResolvedValue({
      device: { id: "d1" },
      token: "stored-token",
    });
    mockGetInfo.mockResolvedValue({ computerName: "Vurain-PC" });
  });

  it("points the API client at the current origin", async () => {
    window.history.replaceState({}, "", "/companion");
    await bootstrapCompanionSession();
    expect(mockSetApiBaseUrl).toHaveBeenCalledWith(window.location.origin);
  });

  it("pairs from the URL token and stores the device token", async () => {
    window.history.replaceState({}, "", "/companion?token=pair-1");

    const result = await bootstrapCompanionSession();

    expect(result.state).toBe("ready");
    expect(mockConfirmPairing).toHaveBeenCalledWith(
      expect.objectContaining({ token: "pair-1" }),
    );
    expect(getCompanionToken()).toBe("stored-token");
    expect(window.location.search).not.toContain("token");
  });

  it("is unpaired without a token", async () => {
    window.history.replaceState({}, "", "/companion");
    expect((await bootstrapCompanionSession()).state).toBe("unpaired");
  });

  it("is ready when a stored token still reaches the computer", async () => {
    window.history.replaceState({}, "", "/companion");
    setCompanionToken("existing-token");

    const result = await bootstrapCompanionSession();

    expect(result.state).toBe("ready");
    expect(mockGetInfo).toHaveBeenCalled();
  });

  it("becomes unpaired and drops the token when the device was removed (401)", async () => {
    window.history.replaceState({}, "", "/companion");
    setCompanionToken("revoked-token");
    mockGetInfo.mockRejectedValueOnce(new FakeApiError(401));

    const result = await bootstrapCompanionSession();

    expect(result.state).toBe("unpaired");
    expect(getCompanionToken()).toBeNull();
  });

  it("reports an error but keeps the token when the computer is unreachable", async () => {
    window.history.replaceState({}, "", "/companion");
    setCompanionToken("still-valid");
    mockGetInfo.mockRejectedValueOnce(new TypeError("Failed to fetch"));

    const result = await bootstrapCompanionSession();

    expect(result.state).toBe("error");
    expect(result.error).toBeTruthy();
    // The token is kept so a later retry can reconnect without re-pairing.
    expect(getCompanionToken()).toBe("still-valid");
  });

  it("reports an error when pairing fails", async () => {
    window.history.replaceState({}, "", "/companion?token=bad");
    mockConfirmPairing.mockRejectedValueOnce(new Error("nope"));

    const result = await bootstrapCompanionSession();

    expect(result.state).toBe("error");
    expect(getCompanionToken()).toBeNull();
  });
});
