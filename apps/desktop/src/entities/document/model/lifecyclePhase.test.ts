import { describe, expect, it } from "vitest";

import { resolveDocumentPhase, documentPhaseClass } from "./lifecyclePhase";

describe("resolveDocumentPhase", () => {
  it.each([
    ["Indexed", "ready"],
    ["Failed", "failed"],
    ["Cancelled", "failed"],
    ["Processing", "processing"],
    ["Chunking", "processing"],
    ["Embedding", "processing"],
    ["Pending", "waiting"],
    ["Queued", "waiting"],
    ["Uploaded", "waiting"],
    ["Draft", "waiting"],
  ])("maps %s to the %s phase", (status, phase) => {
    expect(resolveDocumentPhase(status).phase).toBe(phase);
  });

  it("falls back to accepted for unknown statuses", () => {
    expect(resolveDocumentPhase("something-new").phase).toBe("accepted");
  });

  it("gives every phase a human label", () => {
    for (const status of ["Indexed", "Failed", "Processing", "Queued", "?"]) {
      expect(resolveDocumentPhase(status).label.length).toBeGreaterThan(0);
    }
  });
});

describe("documentPhaseClass", () => {
  it("uses distinct colors for ready and failed", () => {
    expect(documentPhaseClass("ready")).not.toBe(documentPhaseClass("failed"));
    expect(documentPhaseClass("ready")).toContain("green");
  });
});
