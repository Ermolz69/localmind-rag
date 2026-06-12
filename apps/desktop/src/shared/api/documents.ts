import type {
  OperationData,
  OperationPath,
  OperationQuery,
} from "@shared/contracts";
import { toQueryString } from "./common";
import { request } from "./http";

export const documentsApi = {
  getDocuments: (params: OperationQuery<"ListDocuments"> = {}) =>
    request<OperationData<"ListDocuments">>(
      `/documents${toQueryString({
        bucketId: params.bucketId,
        status: params.status,
        cursor: params.cursor,
        limit: params.limit,
      })}`,
    ),

  uploadDocument: (
    file: File,
    bucketId?: OperationQuery<"UploadDocument">["bucketId"],
  ) => {
    const form = new FormData();

    form.append("file", file);

    return request<OperationData<"UploadDocument">>(
      `/documents/upload${toQueryString({ bucketId })}`,
      {
        method: "POST",
        body: form,
      },
    );
  },

  processIngestionJob: (jobId: OperationPath<"ProcessIngestionJob">["id"]) =>
    request<OperationData<"ProcessIngestionJob">>(
      `/ingestion/jobs/${jobId}/process`,
      {
        method: "POST",
      },
    ),

  reindexDocument: (documentId: OperationPath<"ReindexDocument">["id"]) =>
    request<OperationData<"ReindexDocument">>(
      `/documents/${documentId}/reindex`,
      {
        method: "POST",
      },
    ),
};
