import { companionApi, getErrorMessage, setApiBaseUrl } from "@shared/api";
import {
  describeCompanionDevice,
  getCompanionToken,
  setCompanionToken,
} from "@shared/lib/companionAuth";

export type CompanionBootstrapState = "ready" | "unpaired" | "error";

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

  return { state: getCompanionToken() ? "ready" : "unpaired" };
}
