import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { CompanionLivePanel } from "./CompanionLivePanel";

const { mockUseCompanionActivity } = vi.hoisted(() => ({
  mockUseCompanionActivity: vi.fn(),
}));

vi.mock("../model/useCompanionActivity", () => ({
  useCompanionActivity: mockUseCompanionActivity,
}));

function renderPanel() {
  return render(
    <MemoryRouter>
      <CompanionLivePanel />
    </MemoryRouter>,
  );
}

function event(id: string, message: string, timestamp: string) {
  return {
    id,
    kind: "document.added",
    message,
    detail: null,
    timestamp,
  };
}

describe("CompanionLivePanel", () => {
  beforeEach(() => {
    mockUseCompanionActivity.mockReset();
  });

  it("renders nothing while loading or on error", () => {
    mockUseCompanionActivity.mockReturnValue({
      events: [],
      isLoading: true,
      error: null,
    });
    const { container, rerender } = renderPanel();
    expect(container.textContent).toBe("");

    mockUseCompanionActivity.mockReturnValue({
      events: [],
      isLoading: false,
      error: "boom",
    });
    rerender(
      <MemoryRouter>
        <CompanionLivePanel />
      </MemoryRouter>,
    );
    expect(container.textContent).toBe("");
  });

  it("shows an idle message and a View all link when there is no activity", () => {
    mockUseCompanionActivity.mockReturnValue({
      events: [],
      isLoading: false,
      error: null,
    });
    renderPanel();

    expect(screen.getByText(/All caught up/)).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "View all" })).toHaveAttribute(
      "href",
      "/companion/activity",
    );
  });

  it("shows the most recent events, capped to three", () => {
    mockUseCompanionActivity.mockReturnValue({
      events: [
        event("1", "lecture.pdf indexed successfully", "2026-06-20T12:33:00Z"),
        event(
          "2",
          "Creating embeddings for lecture.pdf",
          "2026-06-20T12:32:00Z",
        ),
        event("3", "lecture.pdf added", "2026-06-20T12:30:00Z"),
        event("4", "old.pdf added", "2026-06-20T09:00:00Z"),
      ],
      isLoading: false,
      error: null,
    });
    renderPanel();

    expect(
      screen.getByText("lecture.pdf indexed successfully"),
    ).toBeInTheDocument();
    expect(screen.getByText("lecture.pdf added")).toBeInTheDocument();
    // The fourth (oldest) event is beyond the preview cap.
    expect(screen.queryByText("old.pdf added")).toBeNull();
  });
});
