import type { BucketDto, CreateBucketRequest } from "@entities/bucket";
import { request } from "./http";

export const bucketsApi = {
  getBuckets: () => request<BucketDto[]>("/api/buckets"),
  createBucket: (payload: CreateBucketRequest) =>
    request<BucketDto>("/api/buckets", {
      method: "POST",
      body: JSON.stringify(payload),
    }),
};
