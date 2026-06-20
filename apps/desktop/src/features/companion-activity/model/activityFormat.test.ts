import { describe, expect, it } from "vitest";

import { activityDotClass, formatActivityTime } from "./activityFormat";

describe("activityDotClass", () => {
  it("uses green for indexed and a destructive color for failures", () => {
    expect(activityDotClass("ingestion.indexed")).toContain("green");
    expect(activityDotClass("ingestion.failed")).toContain("destructive");
  });

  it("highlights new content (added / watched-found) with the primary color", () => {
    expect(activityDotClass("document.added")).toContain("primary");
    expect(activityDotClass("watched.found")).toContain("primary");
  });

  it("falls back to a muted dot for other kinds", () => {
    expect(activityDotClass("device.connected")).toContain("muted");
  });
});

describe("formatActivityTime", () => {
  it("renders an hour:minute time", () => {
    expect(formatActivityTime("2026-06-20T12:30:00Z")).toMatch(/\d{1,2}:\d{2}/);
  });
});
