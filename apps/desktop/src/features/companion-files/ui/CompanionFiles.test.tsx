import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { CompanionFiles } from "./CompanionFiles";

const { mockUseCompanionFiles, mockBrowse, mockGoToRoots, mockAddFile } =
  vi.hoisted(() => ({
    mockUseCompanionFiles: vi.fn(),
    mockBrowse: vi.fn(),
    mockGoToRoots: vi.fn(),
    mockAddFile: vi.fn(),
  }));

vi.mock("../model/useCompanionFiles", () => ({
  useCompanionFiles: mockUseCompanionFiles,
}));

const roots = [{ name: "Study", path: "C:\\Users\\me\\Study" }];

describe("CompanionFiles", () => {
  beforeEach(() => {
    mockBrowse.mockReset();
    mockGoToRoots.mockReset();
    mockAddFile.mockReset();
    mockUseCompanionFiles.mockReturnValue({
      roots,
      current: {
        path: "C:\\Users\\me\\Study\\AI\\Lectures",
        parentPath: "C:\\Users\\me\\Study\\AI",
        entries: [
          {
            name: "lecture-01.pdf",
            path: "C:\\Users\\me\\Study\\AI\\Lectures\\lecture-01.pdf",
            isDirectory: false,
          },
        ],
      },
      isLoading: false,
      error: null,
      addingPath: null,
      browse: mockBrowse,
      goToRoots: mockGoToRoots,
      addFile: mockAddFile,
    });
  });

  it("renders a friendly breadcrumb relative to the allowed root", () => {
    render(<CompanionFiles />);

    expect(screen.getByRole("button", { name: "Study" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "AI" })).toBeInTheDocument();
    // The current folder is shown as plain text, not a navigable link.
    expect(screen.queryByRole("button", { name: "Lectures" })).toBeNull();
    expect(screen.getByText("Lectures")).toBeInTheDocument();
  });

  it("navigates when a parent crumb is clicked", async () => {
    const user = userEvent.setup();
    render(<CompanionFiles />);

    await user.click(screen.getByRole("button", { name: "AI" }));

    expect(mockBrowse).toHaveBeenCalledWith("C:\\Users\\me\\Study\\AI");
  });

  it("adds a file by path without leaving the folder", async () => {
    mockAddFile.mockResolvedValue({ success: true, message: "Added" });
    const user = userEvent.setup();
    render(<CompanionFiles />);

    await user.click(screen.getByRole("button", { name: /Add/ }));

    expect(mockAddFile).toHaveBeenCalledWith(
      "C:\\Users\\me\\Study\\AI\\Lectures\\lecture-01.pdf",
    );
  });
});
