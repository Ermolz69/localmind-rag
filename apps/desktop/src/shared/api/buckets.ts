import type { BucketDto, CreateBucketRequest } from "@entities/bucket";

import type { CursorPage } from "./common";
import { toQueryString } from "./common";
import { request } from "./http";

export const bucketsApi = {
  getBuckets: () => request<BucketDto[]>("/buckets"),

  getBucketsPage: ({
    query,
    cursor,
    limit = 30,
  }: {
    query?: string | null;
    cursor?: string | null;
    limit?: number;
  }) =>
    request<CursorPage<BucketDto>>(
      `/buckets/page${toQueryString({ query, cursor, limit })}`,
    ),

  createBucket: (payload: CreateBucketRequest) =>
    request<BucketDto>("/buckets", {
      method: "POST",
      body: JSON.stringify(payload),
    }),
};
