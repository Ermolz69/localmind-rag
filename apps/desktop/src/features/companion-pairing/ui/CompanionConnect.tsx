import { useEffect, useState } from "react";
import { ExternalLink, Smartphone } from "lucide-react";
import { Link } from "react-router-dom";

import { Button, Switch } from "@shared/ui";

import { ConnectPhoneDialog } from "./ConnectPhoneDialog";
import {
  useCompanionPairing,
  type CompanionDevicePermissions,
} from "../model/useCompanionPairing";

const PERMISSION_ITEMS: {
  key: keyof CompanionDevicePermissions;
  label: string;
}[] = [
  { key: "chat", label: "Chat" },
  { key: "search", label: "Search" },
  { key: "viewDocuments", label: "View documents" },
  { key: "viewStatus", label: "View indexing status" },
  { key: "rescan", label: "Rescan folders" },
  { key: "addFiles", label: "Add files from allowed folders" },
];

// Capabilities a phone can never be granted: they are absent from the permission
// model and their routes are off the gateway allowlist. Shown here so the user can
// see the hard boundary, not just the toggles.
const DENIED_ITEMS: string[] = [
  "Delete documents",
  "Change AI runtime",
  "Change system paths",
  "Manage app settings",
  "Access the whole disk",
];

export function CompanionConnect() {
  const {
    devices,
    session,
    secondsRemaining,
    isStarting,
    error,
    loadDevices,
    startPairing,
    cancelPairing,
    revokeDevice,
    updateDevicePermissions,
  } = useCompanionPairing();
  const [dialogOpen, setDialogOpen] = useState(false);

  useEffect(() => {
    void loadDevices();
  }, [loadDevices]);

  async function handleOpen() {
    setDialogOpen(true);
    await startPairing();
  }

  async function handleClose() {
    setDialogOpen(false);
    await cancelPairing();
  }

  return (
    <div className="mt-4 space-y-4">
      <Button onClick={() => void handleOpen()} disabled={isStarting}>
        <Smartphone className="h-4 w-4" />
        Connect phone
      </Button>

      <div>
        <p className="px-1 text-xs font-semibold uppercase tracking-wide text-muted-foreground">
          Connected devices
        </p>
        {devices.length === 0 ? (
          <p className="mt-2 px-1 text-sm text-muted-foreground">
            No devices connected yet.
          </p>
        ) : (
          <ul className="mt-2 divide-y divide-border rounded-lg border border-border">
            {devices.map((device) => (
              <li key={device.id} className="px-4 py-3">
                <div className="flex items-center justify-between gap-4">
                  <div className="min-w-0">
                    <p className="truncate text-sm font-medium text-foreground">
                      {device.name} / {device.platform}
                    </p>
                    <p className="mt-0.5 text-xs text-muted-foreground">
                      Last seen: {new Date(device.lastSeenAt).toLocaleString()}
                    </p>
                  </div>
                  <Button
                    variant="secondary"
                    onClick={() => void revokeDevice(device.id)}
                  >
                    Disconnect
                  </Button>
                </div>
                <div className="mt-3 space-y-3">
                  <div>
                    <p className="text-xs font-medium text-muted-foreground">
                      Allowed
                    </p>
                    <div className="mt-2 grid grid-cols-2 gap-x-4 gap-y-2">
                      {PERMISSION_ITEMS.map((item) => (
                        <label
                          key={item.key}
                          className="flex items-center justify-between gap-2 text-xs text-foreground"
                        >
                          <span>{item.label}</span>
                          <Switch
                            checked={device.permissions[item.key]}
                            onChange={(value) =>
                              void updateDevicePermissions(device.id, {
                                ...device.permissions,
                                [item.key]: value,
                              })
                            }
                            aria-label={`${item.label} for ${device.name}`}
                          />
                        </label>
                      ))}
                    </div>
                  </div>

                  <div>
                    <p className="text-xs font-medium text-muted-foreground">
                      Never allowed
                    </p>
                    <p className="mt-0.5 text-xs text-muted-foreground">
                      These actions can never be granted to a phone.
                    </p>
                    <div className="mt-2 grid grid-cols-2 gap-x-4 gap-y-2">
                      {DENIED_ITEMS.map((label) => (
                        <label
                          key={label}
                          className="flex items-center justify-between gap-2 text-xs text-muted-foreground"
                        >
                          <span>{label}</span>
                          <Switch
                            checked={false}
                            disabled
                            onChange={() => {}}
                            aria-label={`${label} for ${device.name} (never allowed)`}
                          />
                        </label>
                      ))}
                    </div>
                  </div>
                </div>
              </li>
            ))}
          </ul>
        )}
      </div>

      {error && !dialogOpen ? (
        <p className="text-destructive px-1 text-sm">{error}</p>
      ) : null}

      <Link
        to="/companion?preview=1"
        className="inline-flex items-center gap-1 px-1 text-sm text-muted-foreground hover:text-foreground"
      >
        <ExternalLink className="h-4 w-4" />
        Preview the phone interface
      </Link>

      <ConnectPhoneDialog
        open={dialogOpen}
        session={session}
        secondsRemaining={secondsRemaining}
        isStarting={isStarting}
        error={dialogOpen ? error : null}
        onRegenerate={() => void startPairing()}
        onClose={() => void handleClose()}
      />
    </div>
  );
}
