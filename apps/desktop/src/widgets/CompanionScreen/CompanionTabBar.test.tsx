import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it } from "vitest";

import { CompanionTabBar } from "./CompanionTabBar";

function renderAt(path: string) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <CompanionTabBar />
    </MemoryRouter>,
  );
}

describe("CompanionTabBar", () => {
  it("links every main screen for quick switching", () => {
    renderAt("/companion/chat");

    const expected: [string, string][] = [
      ["Chat", "/companion/chat"],
      ["Search", "/companion/search"],
      ["Docs", "/companion/documents"],
      ["Files", "/companion/files"],
      ["Activity", "/companion/activity"],
    ];

    for (const [name, href] of expected) {
      expect(screen.getByRole("link", { name })).toHaveAttribute("href", href);
    }
  });

  it("marks the current screen as the active tab", () => {
    renderAt("/companion/search");

    expect(screen.getByRole("link", { name: "Search" })).toHaveAttribute(
      "aria-current",
      "page",
    );
    expect(screen.getByRole("link", { name: "Chat" })).not.toHaveAttribute(
      "aria-current",
    );
  });
});
