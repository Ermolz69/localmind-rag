import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { CompanionConnect } from "./CompanionConnect";

const { mockUseCompanionPairing, mockUpdateDevicePermissions } = vi.hoisted(
  () => ({
    mockUseCompanionPairing: vi.fn(),
    mockUpdateDevicePermissions: vi.fn(),
  }),
);

vi.mock("../model/useCompanionPairing", () => ({
  useCompanionPairing: mockUseCompanionPairing,
}));

const device = {
  id: "device-1",
  name: "Redmi Note",
  platform: "Chrome",
  createdAt: "2026-06-20T10:00:00Z",
  lastSeenAt: "2026-06-20T10:00:00Z",
  permissions: {
    chat: true,
    search: true,
    viewDocuments: true,
    viewStatus: true,
    rescan: false,
    addFiles: false,
  },
};

function renderConnect() {
  render(
    <MemoryRouter>
      <CompanionConnect />
    </MemoryRouter>,
  );
}

describe("CompanionConnect", () => {
  beforeEach(() => {
    mockUpdateDevicePermissions.mockReset();
    mockUseCompanionPairing.mockReturnValue({
      devices: [device],
      session: null,
      secondsRemaining: 0,
      isStarting: false,
      error: null,
      loadDevices: vi.fn(),
      startPairing: vi.fn(),
      cancelPairing: vi.fn(),
      revokeDevice: vi.fn(),
      updateDevicePermissions: mockUpdateDevicePermissions,
    });
  });

  it("shows the grantable capabilities with their current state", () => {
    renderConnect();

    expect(screen.getByLabelText("Chat for Redmi Note")).toBeChecked();
    expect(
      screen.getByLabelText("Rescan folders for Redmi Note"),
    ).not.toBeChecked();
    expect(
      screen.getByLabelText("Add files from allowed folders for Redmi Note"),
    ).not.toBeChecked();
  });

  it("lists dangerous actions as never allowed and disabled", () => {
    renderConnect();

    for (const label of [
      "Delete documents",
      "Change AI runtime",
      "Change system paths",
      "Manage app settings",
      "Access the whole disk",
    ]) {
      expect(screen.getByText(label)).toBeInTheDocument();
      expect(
        screen.getByLabelText(`${label} for Redmi Note (never allowed)`),
      ).toBeDisabled();
    }
  });

  it("persists a permission change when a toggle is flipped", async () => {
    const user = userEvent.setup();
    renderConnect();

    await user.click(screen.getByLabelText("Rescan folders for Redmi Note"));

    expect(mockUpdateDevicePermissions).toHaveBeenCalledWith("device-1", {
      ...device.permissions,
      rescan: true,
    });
  });
});
