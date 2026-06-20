import { act, renderHook, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { useCompanionFiles } from "./useCompanionFiles";

const { mockGetFileRoots, mockBrowseFiles, mockAddFile } = vi.hoisted(() => ({
  mockGetFileRoots: vi.fn(),
  mockBrowseFiles: vi.fn(),
  mockAddFile: vi.fn(),
}));

vi.mock("@shared/api", () => ({
  companionApi: {
    getFileRoots: mockGetFileRoots,
    browseFiles: mockBrowseFiles,
    addFile: mockAddFile,
  },
  getErrorMessage: (_error: unknown, fallback: string) => fallback,
}));

const browseResponse = {
  path: "C:/Docs",
  parentPath: null,
  entries: [
    { name: "AI", path: "C:/Docs/AI", isDirectory: true },
    { name: "notes.txt", path: "C:/Docs/notes.txt", isDirectory: false },
  ],
};

describe("useCompanionFiles", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockGetFileRoots.mockResolvedValue({
      roots: [{ name: "Docs", path: "C:/Docs" }],
    });
    mockBrowseFiles.mockResolvedValue(browseResponse);
    mockAddFile.mockResolvedValue({ documentId: "doc-1", status: "Queued" });
  });

  it("loads allowed roots on mount", async () => {
    const { result } = renderHook(() => useCompanionFiles());
    await waitFor(() => expect(result.current.roots).toHaveLength(1));
    expect(result.current.current).toBeNull();
  });

  it("browses into a folder and back to roots", async () => {
    const { result } = renderHook(() => useCompanionFiles());
    await waitFor(() => expect(result.current.roots).toHaveLength(1));

    await act(async () => {
      await result.current.browse("C:/Docs");
    });
    expect(mockBrowseFiles).toHaveBeenCalledWith("C:/Docs");
    expect(result.current.current?.entries).toHaveLength(2);

    act(() => result.current.goToRoots());
    expect(result.current.current).toBeNull();
  });

  it("adds a file and reports success", async () => {
    const { result } = renderHook(() => useCompanionFiles());
    await waitFor(() => expect(result.current.roots).toHaveLength(1));

    let outcome:
      | { success: boolean; message: string; documentId?: string }
      | undefined;
    await act(async () => {
      outcome = await result.current.addFile("C:/Docs/notes.txt");
    });

    expect(mockAddFile).toHaveBeenCalledWith({ path: "C:/Docs/notes.txt" });
    expect(outcome?.success).toBe(true);
    // The created document id is surfaced so the UI can track its processing.
    expect(outcome?.documentId).toBe("doc-1");
  });

  it("reports a failed add", async () => {
    mockAddFile.mockRejectedValueOnce(new Error("nope"));
    const { result } = renderHook(() => useCompanionFiles());
    await waitFor(() => expect(result.current.roots).toHaveLength(1));

    let outcome: { success: boolean; message: string } | undefined;
    await act(async () => {
      outcome = await result.current.addFile("C:/Docs/notes.txt");
    });

    expect(outcome?.success).toBe(false);
    expect(outcome?.message).toBe("Could not add this file.");
  });
});
