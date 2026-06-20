import { render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";

import { CompanionPage } from "./index";

vi.mock("@shared/api", () => ({
  companionApi: {
    getInfo: vi.fn().mockResolvedValue({ computerName: "Vurain-PC" }),
    getPairingStatus: vi.fn(),
    startPairing: vi.fn(),
    cancelPairing: vi.fn(),
    confirmPairing: vi.fn(),
    getDevices: vi.fn().mockResolvedValue({ devices: [] }),
    revokeDevice: vi.fn(),
  },
  getErrorMessage: (_error: unknown, fallback: string) => fallback,
}));

function renderAt(path: string) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <CompanionPage />
    </MemoryRouter>,
  );
}

describe("CompanionPage", () => {
  it("renders the companion title and all quick actions", () => {
    renderAt("/companion");

    expect(screen.getByText("LocalMind Companion")).toBeInTheDocument();

    for (const label of [
      "Chat",
      "Search",
      "Documents",
      "Indexing",
      "Folders",
    ]) {
      expect(
        screen.getByRole("link", { name: new RegExp(label) }),
      ).toBeInTheDocument();
    }
  });

  it("links each action to its companion route", () => {
    renderAt("/companion");

    expect(screen.getByRole("link", { name: /Chat/ })).toHaveAttribute(
      "href",
      "/companion/chat",
    );
    expect(screen.getByRole("link", { name: /Folders/ })).toHaveAttribute(
      "href",
      "/companion/folders",
    );
  });

  it("shows the connected computer name once loaded", async () => {
    renderAt("/companion");

    await waitFor(() =>
      expect(screen.getByText("Vurain-PC")).toBeInTheDocument(),
    );
  });

  it("shows an exit-preview link only in preview mode", () => {
    const { unmount } = renderAt("/companion");
    expect(
      screen.queryByRole("link", { name: /Exit preview/ }),
    ).not.toBeInTheDocument();
    unmount();

    renderAt("/companion?preview=1");
    expect(screen.getByRole("link", { name: /Exit preview/ })).toHaveAttribute(
      "href",
      "/",
    );
  });
});
