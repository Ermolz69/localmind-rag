import { useCallback, useEffect, useRef, useState } from "react";

import type { OperationData } from "@shared/contracts";
import { companionApi, getErrorMessage } from "@shared/api";

export type CompanionDevice =
  OperationData<"GetCompanionDevices">["devices"][number];
export type CompanionPairingSession = OperationData<"StartCompanionPairing">;

/**
 * Orchestrates the Companion Mode pairing lifecycle: starting a session (QR),
 * counting down its validity, cancelling it, and listing/revoking trusted
 * devices. Pairing state lives on the backend; this hook mirrors it for the UI.
 */
export function useCompanionPairing() {
  const [devices, setDevices] = useState<CompanionDevice[]>([]);
  const [session, setSession] = useState<CompanionPairingSession | null>(null);
  const [secondsRemaining, setSecondsRemaining] = useState(0);
  const [isStarting, setIsStarting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const intervalRef = useRef<number | null>(null);

  const clearCountdown = useCallback(() => {
    if (intervalRef.current !== null) {
      window.clearInterval(intervalRef.current);
      intervalRef.current = null;
    }
  }, []);

  const loadDevices = useCallback(async () => {
    try {
      const result = await companionApi.getDevices();
      setDevices(result.devices);
    } catch (exception) {
      setError(getErrorMessage(exception, "Unable to load connected devices."));
    }
  }, []);

  const startPairing = useCallback(async () => {
    setError(null);
    setIsStarting(true);
    try {
      const result = await companionApi.startPairing();
      setSession(result);
      setSecondsRemaining(result.expiresInSeconds);
    } catch (exception) {
      setError(getErrorMessage(exception, "Unable to start pairing."));
    } finally {
      setIsStarting(false);
    }
  }, []);

  const cancelPairing = useCallback(async () => {
    clearCountdown();
    setSession(null);
    setSecondsRemaining(0);
    try {
      await companionApi.cancelPairing();
    } catch {
      // Best-effort: the session expires on its own regardless.
    }
  }, [clearCountdown]);

  const revokeDevice = useCallback(
    async (deviceId: string) => {
      setError(null);
      try {
        await companionApi.revokeDevice(deviceId);
        await loadDevices();
      } catch (exception) {
        setError(getErrorMessage(exception, "Unable to disconnect device."));
      }
    },
    [loadDevices],
  );

  // Count down the active session and drop it once it expires.
  useEffect(() => {
    if (!session) {
      clearCountdown();
      return;
    }

    intervalRef.current = window.setInterval(() => {
      setSecondsRemaining((current) => {
        if (current <= 1) {
          clearCountdown();
          setSession(null);
          return 0;
        }

        return current - 1;
      });
    }, 1000);

    return clearCountdown;
  }, [session, clearCountdown]);

  return {
    devices,
    session,
    secondsRemaining,
    isStarting,
    error,
    loadDevices,
    startPairing,
    cancelPairing,
    revokeDevice,
  };
}
