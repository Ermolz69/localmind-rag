import { renderHook, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { useCompanionActivity } from "./useCompanionActivity";

const { mockGetActivity } = vi.hoisted(() => ({
  mockGetActivity: vi.fn(),
}));

vi.mock("@shared/api", () => ({
  companionApi: { getActivity: mockGetActivity },
  getErrorMessage: (_error: unknown, fallback: string) => fallback,
}));

const events = [
  {
    id: "e1",
    timestamp: "2026-06-20T12:33:00Z",
    kind: "ingestion.indexed",
    message: "math.pdf indexed successfully",
    detail: null,
  },
  {
    id: "e2",
    timestamp: "2026-06-20T12:30:00Z",
    kind: "document.added",
    message: "math.pdf added",
    detail: null,
  },
];

describe("useCompanionActivity", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockGetActivity.mockResolvedValue({ events });
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it("loads recent activity on mount", async () => {
    const { result } = renderHook(() => useCompanionActivity());

    await waitFor(() => expect(result.current.events).toHaveLength(2));
    expect(result.current.events[0].message).toBe(
      "math.pdf indexed successfully",
    );
  });

  it("surfaces a load error", async () => {
    mockGetActivity.mockRejectedValueOnce(new Error("offline"));
    const { result } = renderHook(() => useCompanionActivity());

    await waitFor(() =>
      expect(result.current.error).toBe("Unable to load activity."),
    );
  });
});
