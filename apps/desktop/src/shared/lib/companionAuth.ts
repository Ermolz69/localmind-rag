const TOKEN_KEY = "localmind.companion.deviceToken";

/** Returns the stored per-device companion token, or null. */
export function getCompanionToken(): string | null {
  try {
    return window.localStorage.getItem(TOKEN_KEY);
  } catch {
    return null;
  }
}

export function setCompanionToken(token: string): void {
  try {
    window.localStorage.setItem(TOKEN_KEY, token);
  } catch {
    // Storage may be unavailable (private mode); pairing simply won't persist.
  }
}

export function clearCompanionToken(): void {
  try {
    window.localStorage.removeItem(TOKEN_KEY);
  } catch {
    // Ignore.
  }
}

/** Best-effort device name/platform for pairing, derived from the user agent. */
export function describeCompanionDevice(): { name: string; platform: string } {
  const ua = typeof navigator === "undefined" ? "" : navigator.userAgent;

  const platform = /Edg/.test(ua)
    ? "Edge"
    : /Chrome/.test(ua)
      ? "Chrome"
      : /Firefox/.test(ua)
        ? "Firefox"
        : /Safari/.test(ua)
          ? "Safari"
          : "Browser";

  const name = /iPhone/.test(ua)
    ? "iPhone"
    : /iPad/.test(ua)
      ? "iPad"
      : /Android/.test(ua)
        ? "Android phone"
        : "Phone";

  return { name, platform };
}
