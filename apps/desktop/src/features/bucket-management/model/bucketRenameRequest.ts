import type { BucketDto } from "@entities/bucket";
import type { OperationJsonBody } from "@shared/contracts";

export function buildBucketRenameRequest(
  bucket: BucketDto,
  newName: string,
): OperationJsonBody<"UpdateBucket"> {
  return {
    name: newName,
    description: bucket.description,
  };
}
