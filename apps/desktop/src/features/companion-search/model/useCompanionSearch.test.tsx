import { act, renderHook } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { useCompanionSearch } from "./useCompanionSearch";

const { mockSemanticSearch } = vi.hoisted(() => ({
  mockSemanticSearch: vi.fn(),
}));

vi.mock("@shared/api", () => ({
  searchApi: { semanticSearch: mockSemanticSearch },
  getErrorMessage: (_error: unknown, fallback: string) => fallback,
}));

const source = {
  chunkId: "chunk-1",
  documentId: "doc-1",
  documentName: "Notes.pdf",
  pageNumber: 3,
  score: 0.92,
  snippet: "A relevant excerpt.",
};

describe("useCompanionSearch", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockSemanticSearch.mockResolvedValue([source]);
  });

  it("starts empty", () => {
    const { result } = renderHook(() => useCompanionSearch());
    expect(result.current.results).toBeNull();
  });

  it("runs a search and stores results", async () => {
    const { result } = renderHook(() => useCompanionSearch());

    act(() => result.current.setQuery("notes"));
    await act(async () => {
      await result.current.runSearch();
    });

    expect(mockSemanticSearch).toHaveBeenCalledWith("notes");
    expect(result.current.results).toEqual([source]);
  });

  it("does not search a blank query", async () => {
    const { result } = renderHook(() => useCompanionSearch());

    await act(async () => {
      await result.current.runSearch();
    });

    expect(mockSemanticSearch).not.toHaveBeenCalled();
  });

  it("surfaces an error and clears results on failure", async () => {
    mockSemanticSearch.mockRejectedValueOnce(new Error("boom"));
    const { result } = renderHook(() => useCompanionSearch());

    act(() => result.current.setQuery("notes"));
    await act(async () => {
      await result.current.runSearch();
    });

    expect(result.current.error).toBe("Search failed.");
    expect(result.current.results).toBeNull();
  });
});
