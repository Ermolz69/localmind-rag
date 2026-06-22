import {
  ApiError,
  companionApi,
  getErrorMessage,
  setApiBaseUrl,
} from "@shared/api";
import {
  clearCompanionToken,
  describeCompanionDevice,
  getCompanionToken,
  setCompanionToken,
} from "@shared/lib/companionAuth";

export type CompanionBootstrapState = "ready" | "unpaired" | "error";

const COMPUTER_UNAVAILABLE =
  "Your computer isn't reachable. Make sure LocalMind is running with Companion Mode on, and that your phone is on the same Wi-Fi.";

export type CompanionBootstrapResult = {
  state: CompanionBootstrapState;
  error?: string;
};

/**
 * Prepares the phone companion session: points the API client at this origin
 * (the LAN gateway), completes pairing from a `?token=` QR parameter, and stores
 * the resulting device token. Returns whether the companion UI can run.
 */
export async function bootstrapCompanionSession(): Promise<CompanionBootstrapResult> {
  setApiBaseUrl(window.location.origin);

  const url = new URL(window.location.href);
  const pairingToken = url.searchParams.get("token");

  if (pairingToken) {
    const device = describeCompanionDevice();
    try {
      const result = await companionApi.confirmPairing({
        token: pairingToken,
        deviceName: device.name,
        platform: device.platform,
      });
      setCompanionToken(result.token);
      url.searchParams.delete("token");
      window.history.replaceState({}, "", `${url.pathname}${url.search}`);
      return { state: "ready" };
    } catch (exception) {
      return {
        state: "error",
        error: getErrorMessage(
          exception,
          "Pairing failed. Scan a fresh code from the computer.",
        ),
      };
    }
  }

  if (!getCompanionToken()) {
    return { state: "unpaired" };
  }

  // A returning device has a stored token. Verify the computer is actually
  // reachable before showing the UI, so we can tell "computer unavailable" apart
  // from "this device was removed".
  try {
    await companionApi.getInfo();
    return { state: "ready" };
  } catch (exception) {
    if (exception instanceof ApiError && exception.status === 401) {
      clearCompanionToken();
      return { state: "unpaired" };
    }

    return { state: "error", error: COMPUTER_UNAVAILABLE };
  }
}
