import { useEffect, useState } from "react";
import { ExternalLink, Smartphone } from "lucide-react";
import { Link } from "react-router-dom";

import { Button } from "@shared/ui";

import { ConnectPhoneDialog } from "./ConnectPhoneDialog";
import { useCompanionPairing } from "../model/useCompanionPairing";

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
              <li
                key={device.id}
                className="flex items-center justify-between gap-4 px-4 py-3"
              >
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
