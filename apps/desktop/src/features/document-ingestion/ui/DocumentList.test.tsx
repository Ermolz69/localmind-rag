import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";

import type { DocumentSummary } from "@entities/document";

import { DocumentList } from "./DocumentList";

const doc = {
  id: "doc-1",
  name: "report.pdf",
  status: "Indexed",
  createdAt: "2026-06-21T10:00:00Z",
  lastError: null,
} as unknown as DocumentSummary;

function renderList(props: Partial<Parameters<typeof DocumentList>[0]> = {}) {
  render(
    <DocumentList
      documents={[doc]}
      isLoading={false}
      processingDocumentId={null}
      hasMore={false}
      isLoadingMore={false}
      onProcess={vi.fn()}
      onLoadMore={vi.fn()}
      {...props}
    />,
  );
}

describe("DocumentList delete", () => {
  it("renders no delete button when onDelete is not provided", () => {
    renderList();

    expect(
      screen.queryByRole("button", { name: "Delete report.pdf" }),
    ).toBeNull();
  });

  it("confirms before invoking onDelete", async () => {
    const onDelete = vi.fn();
    const user = userEvent.setup();
    renderList({ onDelete });

    await user.click(screen.getByRole("button", { name: "Delete report.pdf" }));

    // A confirmation is required first — onDelete must not fire yet.
    expect(screen.getByText("Delete document?")).toBeInTheDocument();
    expect(onDelete).not.toHaveBeenCalled();

    await user.click(screen.getByRole("button", { name: "Delete" }));

    expect(onDelete).toHaveBeenCalledWith(doc);
  });

  it("lets the user cancel without deleting", async () => {
    const onDelete = vi.fn();
    const user = userEvent.setup();
    renderList({ onDelete });

    await user.click(screen.getByRole("button", { name: "Delete report.pdf" }));
    await user.click(screen.getByRole("button", { name: "Cancel" }));

    expect(onDelete).not.toHaveBeenCalled();
  });
});
