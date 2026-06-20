import { act, renderHook, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { useCompanionPairing } from "./useCompanionPairing";

const {
  mockGetDevices,
  mockStartPairing,
  mockCancelPairing,
  mockRevokeDevice,
  mockGetErrorMessage,
} = vi.hoisted(() => ({
  mockGetDevices: vi.fn(),
  mockStartPairing: vi.fn(),
  mockCancelPairing: vi.fn(),
  mockRevokeDevice: vi.fn(),
  mockGetErrorMessage: vi.fn((_error: unknown, fallback: string) => fallback),
}));

vi.mock("@shared/api", () => ({
  companionApi: {
    getDevices: mockGetDevices,
    startPairing: mockStartPairing,
    cancelPairing: mockCancelPairing,
    revokeDevice: mockRevokeDevice,
  },
  getErrorMessage: mockGetErrorMessage,
}));

const device = {
  id: "device-1",
  name: "Redmi Note",
  platform: "Chrome",
  createdAt: "2026-06-20T10:00:00Z",
  lastSeenAt: "2026-06-20T10:00:00Z",
};

const session = {
  token: "abc123",
  pairingUrl: "http://192.168.1.50:49322/companion/pair?token=abc123",
  expiresAt: "2026-06-20T10:05:00Z",
  expiresInSeconds: 300,
};

describe("useCompanionPairing", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockGetDevices.mockResolvedValue({ devices: [] });
    mockStartPairing.mockResolvedValue(session);
    mockCancelPairing.mockResolvedValue(undefined);
    mockRevokeDevice.mockResolvedValue(undefined);
  });

  it("starts with no session and no devices", () => {
    const { result } = renderHook(() => useCompanionPairing());
    expect(result.current.session).toBeNull();
    expect(result.current.devices).toEqual([]);
  });

  it("loads trusted devices", async () => {
    mockGetDevices.mockResolvedValueOnce({ devices: [device] });
    const { result } = renderHook(() => useCompanionPairing());

    await act(async () => {
      await result.current.loadDevices();
    });

    expect(result.current.devices).toEqual([device]);
  });

  it("starts a pairing session and seeds the countdown", async () => {
    const { result } = renderHook(() => useCompanionPairing());

    await act(async () => {
      await result.current.startPairing();
    });

    expect(result.current.session).toEqual(session);
    expect(result.current.secondsRemaining).toBe(300);
  });

  it("cancels the active session", async () => {
    const { result } = renderHook(() => useCompanionPairing());

    await act(async () => {
      await result.current.startPairing();
    });
    await act(async () => {
      await result.current.cancelPairing();
    });

    expect(result.current.session).toBeNull();
    expect(result.current.secondsRemaining).toBe(0);
    expect(mockCancelPairing).toHaveBeenCalledOnce();
  });

  it("revokes a device and refreshes the list", async () => {
    mockGetDevices.mockResolvedValue({ devices: [] });
    const { result } = renderHook(() => useCompanionPairing());

    await act(async () => {
      await result.current.revokeDevice("device-1");
    });

    expect(mockRevokeDevice).toHaveBeenCalledWith("device-1");
    await waitFor(() => expect(result.current.devices).toEqual([]));
  });

  it("surfaces an error when starting pairing fails", async () => {
    mockStartPairing.mockRejectedValueOnce(new Error("nope"));
    const { result } = renderHook(() => useCompanionPairing());

    await act(async () => {
      await result.current.startPairing();
    });

    expect(result.current.error).toBe("Unable to start pairing.");
    expect(result.current.session).toBeNull();
  });
});
