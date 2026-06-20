import { beforeEach, describe, expect, it, vi } from "vitest";

import {
  clearCompanionToken,
  getCompanionToken,
  setCompanionToken,
} from "@shared/lib/companionAuth";

const { mockConfirmPairing, mockSetApiBaseUrl } = vi.hoisted(() => ({
  mockConfirmPairing: vi.fn(),
  mockSetApiBaseUrl: vi.fn(),
}));

vi.mock("@shared/api", () => ({
  setApiBaseUrl: mockSetApiBaseUrl,
  getErrorMessage: (_error: unknown, fallback: string) => fallback,
  companionApi: { confirmPairing: mockConfirmPairing },
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

  it("is ready when a device token is already stored", async () => {
    window.history.replaceState({}, "", "/companion");
    setCompanionToken("existing-token");
    expect((await bootstrapCompanionSession()).state).toBe("ready");
  });

  it("reports an error when pairing fails", async () => {
    window.history.replaceState({}, "", "/companion?token=bad");
    mockConfirmPairing.mockRejectedValueOnce(new Error("nope"));

    const result = await bootstrapCompanionSession();

    expect(result.state).toBe("error");
    expect(getCompanionToken()).toBeNull();
  });
});
