import type { OperationJsonBody, Schema } from "@shared/contracts";

export type BucketDto = Schema<"BucketDto">;
export type CreateBucketRequest = OperationJsonBody<"CreateBucket">;
