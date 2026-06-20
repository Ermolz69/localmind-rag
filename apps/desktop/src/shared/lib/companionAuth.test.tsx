import { afterEach, describe, expect, it, vi } from "vitest";

import {
  clearCompanionToken,
  describeCompanionDevice,
  getCompanionToken,
  setCompanionToken,
} from "./companionAuth";

describe("companion token storage", () => {
  afterEach(() => {
    clearCompanionToken();
    vi.unstubAllGlobals();
  });

  it("stores, reads, and clears the device token", () => {
    expect(getCompanionToken()).toBeNull();
    setCompanionToken("abc123");
    expect(getCompanionToken()).toBe("abc123");
    clearCompanionToken();
    expect(getCompanionToken()).toBeNull();
  });
});

describe("describeCompanionDevice", () => {
  afterEach(() => vi.unstubAllGlobals());

  it("derives name and platform from an Android Chrome user agent", () => {
    vi.stubGlobal("navigator", {
      userAgent:
        "Mozilla/5.0 (Linux; Android 13) AppleWebKit Chrome/120 Mobile",
    });
    expect(describeCompanionDevice()).toEqual({
      name: "Android phone",
      platform: "Chrome",
    });
  });

  it("falls back when the user agent is unknown", () => {
    vi.stubGlobal("navigator", { userAgent: "" });
    expect(describeCompanionDevice()).toEqual({
      name: "Phone",
      platform: "Browser",
    });
  });
});
