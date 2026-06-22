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

describe("DocumentList context menu", () => {
  it("has no visible delete button — delete is in the right-click menu", () => {
    renderList({ onDelete: vi.fn() });

    expect(screen.queryByRole("button", { name: /delete/i })).toBeNull();
  });

  it("right-click opens context menu with Delete option", async () => {
    const onDelete = vi.fn();
    const user = userEvent.setup();
    renderList({ onDelete });

    await user.pointer({
      target: screen.getByText("report.pdf"),
      keys: "[MouseRight]",
    });

    expect(
      screen.getByRole("menuitem", { name: /delete/i }),
    ).toBeInTheDocument();
    expect(onDelete).not.toHaveBeenCalled();
  });

  it("clicking Delete in the context menu opens a confirm dialog", async () => {
    const onDelete = vi.fn();
    const user = userEvent.setup();
    renderList({ onDelete });

    await user.pointer({
      target: screen.getByText("report.pdf"),
      keys: "[MouseRight]",
    });

    await user.click(screen.getByRole("menuitem", { name: /delete/i }));

    expect(screen.getByText("Delete document?")).toBeInTheDocument();
    expect(onDelete).not.toHaveBeenCalled();
  });

  it("confirming the dialog invokes onDelete", async () => {
    const onDelete = vi.fn();
    const user = userEvent.setup();
    renderList({ onDelete });

    await user.pointer({
      target: screen.getByText("report.pdf"),
      keys: "[MouseRight]",
    });
    await user.click(screen.getByRole("menuitem", { name: /delete/i }));
    await user.click(screen.getByRole("button", { name: "Delete" }));

    expect(onDelete).toHaveBeenCalledWith(doc);
  });

  it("cancelling the dialog does not invoke onDelete", async () => {
    const onDelete = vi.fn();
    const user = userEvent.setup();
    renderList({ onDelete });

    await user.pointer({
      target: screen.getByText("report.pdf"),
      keys: "[MouseRight]",
    });
    await user.click(screen.getByRole("menuitem", { name: /delete/i }));
    await user.click(screen.getByRole("button", { name: "Cancel" }));

    expect(onDelete).not.toHaveBeenCalled();
  });

  it("Escape closes the context menu", async () => {
    const user = userEvent.setup();
    renderList({ onDelete: vi.fn() });

    await user.pointer({
      target: screen.getByText("report.pdf"),
      keys: "[MouseRight]",
    });
    expect(screen.getByRole("menu")).toBeInTheDocument();

    await user.keyboard("{Escape}");
    expect(screen.queryByRole("menu")).toBeNull();
  });
});
