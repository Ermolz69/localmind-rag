import { render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { RecentlyAddedFiles } from "./RecentlyAddedFiles";

const { mockUseCompanionDocuments } = vi.hoisted(() => ({
  mockUseCompanionDocuments: vi.fn(),
}));

vi.mock("@features/companion-documents", () => ({
  useCompanionDocuments: mockUseCompanionDocuments,
}));

function setDocuments(
  documents: Array<{
    id: string;
    name: string;
    status: string;
    progressPercent?: number | null;
    errorMessage?: string | null;
  }>,
) {
  mockUseCompanionDocuments.mockReturnValue({
    documents: documents.map((document) => ({
      progressPercent: null,
      errorMessage: null,
      ...document,
    })),
    isLoading: false,
    error: null,
    refresh: vi.fn(),
  });
}

describe("RecentlyAddedFiles", () => {
  beforeEach(() => {
    mockUseCompanionDocuments.mockReset();
  });

  it("reflects the live lifecycle status of an added file", () => {
    setDocuments([{ id: "doc-1", name: "lecture.pdf", status: "Indexed" }]);

    render(
      <RecentlyAddedFiles items={[{ id: "doc-1", name: "lecture.pdf" }]} />,
    );

    expect(screen.getByText("lecture.pdf")).toBeInTheDocument();
    expect(screen.getByText("Ready to search")).toBeInTheDocument();
  });

  it("shows the failure reason when processing failed", () => {
    setDocuments([
      {
        id: "doc-1",
        name: "broken.pdf",
        status: "Failed",
        errorMessage: "Unsupported file",
      },
    ]);

    render(
      <RecentlyAddedFiles items={[{ id: "doc-1", name: "broken.pdf" }]} />,
    );

    expect(screen.getByText("Couldn't process")).toBeInTheDocument();
    expect(screen.getByText("Unsupported file")).toBeInTheDocument();
  });

  it("falls back to Accepted before the backend reports a status", () => {
    setDocuments([]);

    render(<RecentlyAddedFiles items={[{ id: "doc-1", name: "fresh.pdf" }]} />);

    expect(screen.getByText("fresh.pdf")).toBeInTheDocument();
    expect(screen.getByText("Accepted")).toBeInTheDocument();
  });
});
