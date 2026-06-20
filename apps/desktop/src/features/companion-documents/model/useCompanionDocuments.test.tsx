import { renderHook, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { useCompanionDocuments } from "./useCompanionDocuments";

const { mockGetDocuments, mockGetJobs } = vi.hoisted(() => ({
  mockGetDocuments: vi.fn(),
  mockGetJobs: vi.fn(),
}));

vi.mock("@shared/api", () => ({
  documentsApi: { getDocuments: mockGetDocuments },
  ingestionApi: { getJobs: mockGetJobs },
  getErrorMessage: (_error: unknown, fallback: string) => fallback,
}));

const documents = [
  { id: "1", name: "math.pdf", status: "Indexed", createdAt: "2026-06-20" },
  {
    id: "2",
    name: "lecture.docx",
    status: "Processing",
    createdAt: "2026-06-20",
  },
  {
    id: "3",
    name: "broken.pdf",
    status: "Failed",
    createdAt: "2026-06-20",
    lastError: "Could not extract text",
  },
];

const jobs = [
  {
    id: "j2",
    documentId: "2",
    status: "Embedding",
    progressPercent: 75,
    currentStep: "Embedding",
    createdAt: "2026-06-20",
    updatedAt: null,
    errorMessage: null,
  },
  {
    id: "j3",
    documentId: "3",
    status: "Failed",
    progressPercent: 0,
    currentStep: "Failed",
    createdAt: "2026-06-20",
    updatedAt: null,
    errorMessage: "Could not extract text",
  },
];

describe("useCompanionDocuments", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockGetDocuments.mockResolvedValue({ items: documents });
    mockGetJobs.mockResolvedValue({ items: jobs });
  });

  it("merges documents with their ingestion jobs", async () => {
    const { result } = renderHook(() => useCompanionDocuments());

    await waitFor(() => expect(result.current.documents).toHaveLength(3));

    const byName = Object.fromEntries(
      result.current.documents.map((document) => [document.name, document]),
    );

    expect(byName["math.pdf"].status).toBe("Indexed");
    expect(byName["math.pdf"].progressPercent).toBeNull();

    expect(byName["lecture.docx"].status).toBe("Embedding");
    expect(byName["lecture.docx"].progressPercent).toBe(75);

    expect(byName["broken.pdf"].status).toBe("Failed");
    expect(byName["broken.pdf"].errorMessage).toBe("Could not extract text");
  });

  it("surfaces a load error", async () => {
    mockGetDocuments.mockRejectedValueOnce(new Error("offline"));
    const { result } = renderHook(() => useCompanionDocuments());

    await waitFor(() =>
      expect(result.current.error).toBe("Unable to load documents."),
    );
  });
});
