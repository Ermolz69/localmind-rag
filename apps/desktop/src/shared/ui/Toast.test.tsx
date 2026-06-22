import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";

import { Toast } from "./Toast";

describe("Toast", () => {
  it("renders nothing when message is null", () => {
    const { container } = render(<Toast message={null} />);
    expect(container).toBeEmptyDOMElement();
  });

  it("renders the message text when provided", () => {
    render(<Toast message="Saved" />);
    expect(screen.getByText("Saved")).toBeInTheDocument();
  });

  it("announces errors assertively for screen readers", () => {
    render(<Toast message="Something failed" variant="error" />);
    expect(screen.getByRole("status")).toHaveAttribute(
      "aria-live",
      "assertive",
    );
  });

  it("announces non-error variants politely", () => {
    render(<Toast message="Done" variant="success" />);
    expect(screen.getByRole("status")).toHaveAttribute("aria-live", "polite");
  });

  it("does not render a dismiss control without onDismiss", () => {
    render(<Toast message="No close button" />);
    expect(
      screen.queryByRole("button", { name: "Dismiss notification" }),
    ).not.toBeInTheDocument();
  });

  it("invokes onDismiss when the close control is clicked", async () => {
    const onDismiss = vi.fn();
    render(<Toast message="Closable" onDismiss={onDismiss} />);

    await userEvent.click(
      screen.getByRole("button", { name: "Dismiss notification" }),
    );

    expect(onDismiss).toHaveBeenCalledOnce();
  });
});
