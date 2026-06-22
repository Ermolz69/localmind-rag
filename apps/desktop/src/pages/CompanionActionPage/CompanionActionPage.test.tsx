import { render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { describe, expect, it } from "vitest";

import { CompanionActionPage } from "./index";

function renderAt(path: string) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <Routes>
        <Route path="/companion/:action" element={<CompanionActionPage />} />
      </Routes>
    </MemoryRouter>,
  );
}

describe("CompanionActionPage", () => {
  it("renders a known action with a home link", () => {
    renderAt("/companion/chat");

    expect(screen.getByRole("heading", { name: "Chat" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /Home/ })).toHaveAttribute(
      "href",
      "/companion",
    );
  });

  it("handles an unknown action gracefully", () => {
    renderAt("/companion/nope");

    expect(
      screen.getByRole("heading", { name: "Unknown action" }),
    ).toBeInTheDocument();
  });
});
