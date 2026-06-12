import type {
  OperationData,
  OperationPath,
  OperationQuery,
} from "@shared/contracts";
import { toQueryString } from "./common";
import { request } from "./http";

export const ingestionApi = {
  getJobs: (params: OperationQuery<"ListIngestionJobs"> = {}) =>
    request<OperationData<"ListIngestionJobs">>(
      `/ingestion/jobs${toQueryString({
        status: params.status,
        offset: params.offset,
        limit: params.limit,
      })}`,
    ),

  getJob: (jobId: OperationPath<"GetIngestionJob">["id"]) =>
    request<OperationData<"GetIngestionJob">>(`/ingestion/jobs/${jobId}`),

  retryJob: (jobId: OperationPath<"RetryIngestionJob">["id"]) =>
    request<OperationData<"RetryIngestionJob">>(
      `/ingestion/jobs/${jobId}/retry`,
      {
        method: "POST",
      },
    ),

  cancelJob: (jobId: OperationPath<"CancelIngestionJob">["id"]) =>
    request<OperationData<"CancelIngestionJob">>(
      `/ingestion/jobs/${jobId}/cancel`,
      {
        method: "POST",
      },
    ),

  processJob: (jobId: OperationPath<"ProcessIngestionJob">["id"]) =>
    request<OperationData<"ProcessIngestionJob">>(
      `/ingestion/jobs/${jobId}/process`,
      {
        method: "POST",
      },
    ),
};
