import { describe, expect, it } from "vitest";

import { formatBucketSyncStatus } from "./syncStatus";

describe("formatBucketSyncStatus", () => {
  it("formats numeric OpenAPI enum values", () => {
    expect(formatBucketSyncStatus(0)).toBe("Local only");
    expect(formatBucketSyncStatus(1)).toBe("Pending upload");
    expect(formatBucketSyncStatus(7)).toBe("Synced");
  });

  it("formats numeric string values", () => {
    expect(formatBucketSyncStatus("0")).toBe("Local only");
  });

  it("formats string enum names", () => {
    expect(formatBucketSyncStatus("PendingUpload")).toBe("Pending Upload");
    expect(formatBucketSyncStatus("download_failed")).toBe("Download failed");
  });
});
