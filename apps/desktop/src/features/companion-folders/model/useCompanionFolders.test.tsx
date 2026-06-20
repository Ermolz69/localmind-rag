import { act, renderHook, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { useCompanionFolders } from "./useCompanionFolders";

const { mockGetStatus, mockRescan, mockCleanup } = vi.hoisted(() => ({
  mockGetStatus: vi.fn(),
  mockRescan: vi.fn(),
  mockCleanup: vi.fn(),
}));

vi.mock("@shared/api", () => ({
  watchedFoldersApi: {
    getStatus: mockGetStatus,
    rescan: mockRescan,
    cleanup: mockCleanup,
  },
  getErrorMessage: (_error: unknown, fallback: string) => fallback,
}));

const statusResponse = {
  enabled: true,
  debounceMilliseconds: 1000,
  pendingEvents: 0,
  deletePolicy: "MarkDeleted",
  lastError: null,
  lastErrorAt: null,
  folders: [
    {
      path: "C:/Docs",
      enabled: true,
      includeSubdirectories: false,
      exists: true,
      isWatching: true,
      pendingEvents: 0,
      lastEventAt: null,
      lastError: null,
      lastErrorAt: null,
      healthStatus: "Active",
      lastScanStartedAt: null,
      lastScanCompletedAt: null,
      activeDocumentsCount: 4,
      deletedWaitingCleanupCount: 0,
      lastScanNewFiles: 0,
      lastScanChangedFiles: 0,
      lastScanDeletedFiles: 0,
      lastScanUnchangedFiles: 0,
      lastScanUnsupportedFiles: 0,
    },
  ],
};

describe("useCompanionFolders", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockGetStatus.mockResolvedValue(statusResponse);
    mockRescan.mockResolvedValue({
      queuedCreatedOrChanged: 1,
      unchangedFiles: 2,
      unsupportedFiles: 0,
      queuedDeleted: 1,
    });
    mockCleanup.mockResolvedValue({ cleanedCount: 3 });
  });

  it("loads the watched folder status", async () => {
    const { result } = renderHook(() => useCompanionFolders());

    await waitFor(() => expect(result.current.status?.folders).toHaveLength(1));
  });

  it("rescans a single folder and reports the result", async () => {
    const { result } = renderHook(() => useCompanionFolders());
    await waitFor(() => expect(result.current.status).not.toBeNull());

    let outcome: { success: boolean; message: string } | undefined;
    await act(async () => {
      outcome = await result.current.rescan("C:/Docs");
    });

    expect(mockRescan).toHaveBeenCalledWith({ path: "C:/Docs" });
    expect(outcome?.success).toBe(true);
    expect(outcome?.message).toContain("3 file(s) checked");
    expect(outcome?.message).toContain("1 missing");
  });

  it("rescans all folders with a null path", async () => {
    const { result } = renderHook(() => useCompanionFolders());
    await waitFor(() => expect(result.current.status).not.toBeNull());

    await act(async () => {
      await result.current.rescan();
    });

    expect(mockRescan).toHaveBeenCalledWith({ path: null });
  });

  it("cleans up deleted files and reports the count", async () => {
    const { result } = renderHook(() => useCompanionFolders());
    await waitFor(() => expect(result.current.status).not.toBeNull());

    let outcome: { success: boolean; message: string } | undefined;
    await act(async () => {
      outcome = await result.current.cleanup();
    });

    expect(outcome?.success).toBe(true);
    expect(outcome?.message).toContain("3 deleted");
  });

  it("reports a failed rescan", async () => {
    mockRescan.mockRejectedValueOnce(new Error("offline"));
    const { result } = renderHook(() => useCompanionFolders());
    await waitFor(() => expect(result.current.status).not.toBeNull());

    let outcome: { success: boolean; message: string } | undefined;
    await act(async () => {
      outcome = await result.current.rescan("C:/Docs");
    });

    expect(outcome?.success).toBe(false);
    expect(outcome?.message).toBe("Rescan failed.");
  });
});
