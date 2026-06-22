import { QRCodeSVG } from "qrcode.react";

import { Button, Modal } from "@shared/ui";

import type { CompanionPairingSession } from "../model/useCompanionPairing";

type ConnectPhoneDialogProps = {
  open: boolean;
  session: CompanionPairingSession | null;
  secondsRemaining: number;
  isStarting: boolean;
  error: string | null;
  onRegenerate: () => void;
  onClose: () => void;
};

function formatCountdown(totalSeconds: number): string {
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  return `${minutes}:${String(seconds).padStart(2, "0")}`;
}

export function ConnectPhoneDialog({
  open,
  session,
  secondsRemaining,
  isStarting,
  error,
  onRegenerate,
  onClose,
}: ConnectPhoneDialogProps) {
  return (
    <Modal
      open={open}
      title="Connect phone"
      description="Scan this code with your phone to pair it with LocalMind."
      onClose={onClose}
    >
      <div className="flex flex-col items-center gap-4 text-center">
        {session ? (
          <>
            <div className="rounded-lg bg-white p-4">
              <QRCodeSVG value={session.pairingUrl} size={192} />
            </div>
            <p className="text-sm text-muted-foreground">
              Open your phone&apos;s camera and point it at the code.
            </p>
            <p className="text-sm font-medium text-foreground">
              Code valid for {formatCountdown(secondsRemaining)}
            </p>
          </>
        ) : isStarting ? (
          <p className="py-12 text-sm text-muted-foreground">
            Generating a secure code…
          </p>
        ) : (
          <div className="flex flex-col items-center gap-3 py-8">
            <p className="text-sm text-muted-foreground">
              This code has expired. Codes are short-lived for your security.
            </p>
            <Button onClick={onRegenerate}>Generate a new code</Button>
          </div>
        )}

        {error ? <p className="text-destructive text-sm">{error}</p> : null}

        <Button variant="secondary" className="mt-2" onClick={onClose}>
          Done
        </Button>
      </div>
    </Modal>
  );
}
