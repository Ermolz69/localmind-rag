import type {
  OperationData,
  OperationJsonBody,
  OperationPath,
  OperationQuery,
} from "@shared/contracts";
import { toQueryString } from "./common";
import { request } from "./http";

export const bucketsApi = {
  getBuckets: () => request<OperationData<"ListBuckets">>("/buckets"),

  getBucketsPage: ({
    query,
    cursor,
    limit = 30,
  }: OperationQuery<"ListBucketsPage"> = {}) =>
    request<OperationData<"ListBucketsPage">>(
      `/buckets/page${toQueryString({ query, cursor, limit })}`,
    ),

  createBucket: (payload: OperationJsonBody<"CreateBucket">) =>
    request<OperationData<"CreateBucket">>("/buckets", {
      method: "POST",
      body: JSON.stringify(payload),
    }),
  updateBucket: (
    id: OperationPath<"UpdateBucket">["id"],
    payload: OperationJsonBody<"UpdateBucket">,
  ) =>
    request<OperationData<"UpdateBucket">>(`/buckets/${id}`, {
      method: "PUT",
      body: JSON.stringify(payload),
    }),
  deleteBucket: (id: OperationPath<"DeleteBucket">["id"]) =>
    request<OperationData<"DeleteBucket">>(`/buckets/${id}`, {
      method: "DELETE",
    }),
};
