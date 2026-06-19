import { describe, expect, it } from "vitest";
import type { BucketDto } from "@entities/bucket";

import { buildBucketRenameRequest } from "./bucketRenameRequest";

function bucket(description: string | null): BucketDto {
  return {
    id: "bucket-id",
    name: "Old name",
    description,
    syncStatus: 0,
    createdAt: "2026-06-12T00:00:00Z",
    updatedAt: null,
  };
}

describe("buildBucketRenameRequest", () => {
  it("preserves the existing description when renaming", () => {
    expect(
      buildBucketRenameRequest(
        bucket("Description must survive rename"),
        "New name",
      ),
    ).toEqual({
      name: "New name",
      description: "Description must survive rename",
    });
  });

  it("keeps null descriptions null", () => {
    expect(buildBucketRenameRequest(bucket(null), "New name")).toEqual({
      name: "New name",
      description: null,
    });
  });
});
