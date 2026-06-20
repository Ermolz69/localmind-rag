import { act, renderHook, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { createDocumentSummary } from "@test/factories/documentFactories";
import {
  createErrorPreviewResponse,
  createMarkdownPreviewResponse,
  createTextPreviewResponse,
} from "@test/factories/previewFactories";
import { useDocumentPreview } from "./useDocumentPreview";

const { mockGetDocumentPreview, mockGetPreviewFileUrl, mockGetErrorMessage } =
  vi.hoisted(() => ({
    mockGetDocumentPreview: vi.fn(),
    mockGetPreviewFileUrl: vi.fn(
      (previewUrl: string) => `http://localhost${previewUrl}`,
    ),
    mockGetErrorMessage: vi.fn((_err: unknown, fallback: string) => fallback),
  }));

vi.mock("@shared/api", () => ({
  documentsApi: {
    getDocumentPreview: mockGetDocumentPreview,
    getPreviewFileUrl: mockGetPreviewFileUrl,
  },
  getErrorMessage: mockGetErrorMessage,
}));

describe("useDocumentPreview", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("has correct initial state", () => {
    const { result } = renderHook(() => useDocumentPreview());

    expect(result.current.document).toBeNull();
    expect(result.current.preview).toBeNull();
    expect(result.current.error).toBeNull();
    expect(result.current.isLoading).toBe(false);
    expect(result.current.isOpen).toBe(false);
  });

  it("sets isLoading and document while fetching", async () => {
    const doc = createDocumentSummary();
    let resolvePreview!: (
      val: ReturnType<typeof createTextPreviewResponse>,
    ) => void;
    mockGetDocumentPreview.mockReturnValueOnce(
      new Promise<ReturnType<typeof createTextPreviewResponse>>(
        (res) => (resolvePreview = res),
      ),
    );

    const { result } = renderHook(() => useDocumentPreview());

    act(() => {
      void result.current.openPreview(doc);
    });

    expect(result.current.isLoading).toBe(true);
    expect(result.current.isOpen).toBe(true);
    expect(result.current.document).toEqual(doc);
    expect(result.current.preview).toBeNull();

    resolvePreview(createTextPreviewResponse({ documentId: doc.id }));

    await waitFor(() => expect(result.current.isLoading).toBe(false));
    expect(result.current.preview).not.toBeNull();
  });

  it("sets preview on successful fetch", async () => {
    const doc = createDocumentSummary();
    const expected = createTextPreviewResponse({ documentId: doc.id });
    mockGetDocumentPreview.mockResolvedValueOnce(expected);

    const { result } = renderHook(() => useDocumentPreview());

    await act(async () => {
      await result.current.openPreview(doc);
    });

    expect(result.current.preview).toEqual(expected);
    expect(result.current.error).toBeNull();
    expect(result.current.isLoading).toBe(false);
    expect(result.current.isOpen).toBe(true);
  });

  it("sets preview for markdown document", async () => {
    const doc = createDocumentSummary({ name: "notes.md" });
    const expected = createMarkdownPreviewResponse({ documentId: doc.id });
    mockGetDocumentPreview.mockResolvedValueOnce(expected);

    const { result } = renderHook(() => useDocumentPreview());

    await act(async () => {
      await result.current.openPreview(doc);
    });

    expect(result.current.preview?.previewKind).toBe("Markdown");
    expect(result.current.preview?.textContent).toEqual(expected.textContent);
  });

  it("sets error on failed fetch", async () => {
    const doc = createDocumentSummary();
    const apiError = new Error("Network error");
    mockGetDocumentPreview.mockRejectedValueOnce(apiError);
    mockGetErrorMessage.mockReturnValueOnce("Unable to load preview.");

    const { result } = renderHook(() => useDocumentPreview());

    await act(async () => {
      await result.current.openPreview(doc);
    });

    expect(result.current.error).toBe("Unable to load preview.");
    expect(result.current.preview).toBeNull();
    expect(result.current.isLoading).toBe(false);
    expect(result.current.isOpen).toBe(true);
  });

  it("resets all state on closePreview", async () => {
    const doc = createDocumentSummary();
    mockGetDocumentPreview.mockResolvedValueOnce(
      createTextPreviewResponse({ documentId: doc.id }),
    );

    const { result } = renderHook(() => useDocumentPreview());

    await act(async () => {
      await result.current.openPreview(doc);
    });

    expect(result.current.isOpen).toBe(true);

    act(() => {
      result.current.closePreview();
    });

    expect(result.current.document).toBeNull();
    expect(result.current.preview).toBeNull();
    expect(result.current.error).toBeNull();
    expect(result.current.isLoading).toBe(false);
    expect(result.current.isOpen).toBe(false);
  });

  it("discards stale responses from superseded requests", async () => {
    const doc1 = createDocumentSummary({ name: "first.txt" });
    const doc2 = createDocumentSummary({ name: "second.md" });
    const preview2 = createMarkdownPreviewResponse({ documentId: doc2.id });

    let resolveFirst!: (
      v: ReturnType<typeof createErrorPreviewResponse>,
    ) => void;
    mockGetDocumentPreview
      .mockReturnValueOnce(
        new Promise<ReturnType<typeof createErrorPreviewResponse>>(
          (res) => (resolveFirst = res),
        ),
      )
      .mockResolvedValueOnce(preview2);

    const { result } = renderHook(() => useDocumentPreview());

    act(() => {
      void result.current.openPreview(doc1);
    });

    await act(async () => {
      await result.current.openPreview(doc2);
    });

    resolveFirst(createErrorPreviewResponse({ documentId: doc1.id }));

    await waitFor(() => expect(result.current.isLoading).toBe(false));

    expect(result.current.document?.id).toBe(doc2.id);
    expect(result.current.preview?.previewKind).toBe("Markdown");
  });
});
