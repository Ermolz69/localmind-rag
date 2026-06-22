import { render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { clearCompanionToken } from "@shared/lib/companionAuth";

import { CompanionApp } from "./CompanionApp";

vi.mock("@shared/api", () => ({
  setApiBaseUrl: vi.fn(),
  getErrorMessage: (_error: unknown, fallback: string) => fallback,
  companionApi: { confirmPairing: vi.fn() },
}));

describe("CompanionApp", () => {
  beforeEach(() => {
    clearCompanionToken();
    window.history.replaceState({}, "", "/companion");
  });

  it("shows the not-connected screen when there is no token", async () => {
    render(<CompanionApp />);

    await waitFor(() =>
      expect(screen.getByText("Not connected")).toBeInTheDocument(),
    );
  });
});
