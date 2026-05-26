import type {
  DocumentSummary,
  GetDocumentsRequest,
  ProcessIngestionJobResponse,
  ReindexDocumentResponse,
  UploadDocumentResponse,
} from "@entities/document";

import type { CursorPage } from "./common";
import { toQueryString } from "./common";
import { request } from "./http";

export const documentsApi = {
  getDocuments: (params: GetDocumentsRequest = {}) =>
    request<CursorPage<DocumentSummary>>(
      `/documents${toQueryString({
        bucketId: params.bucketId,
        status: params.status,
        cursor: params.cursor,
        limit: params.limit,
      })}`,
    ),

  uploadDocument: (file: File, bucketId?: string | null) => {
    const form = new FormData();

    form.append("file", file);

    return request<UploadDocumentResponse>(
      `/documents/upload${toQueryString({ bucketId })}`,
      {
        method: "POST",
        body: form,
      },
    );
  },

  processIngestionJob: (jobId: string) =>
    request<ProcessIngestionJobResponse>(`/ingestion/jobs/${jobId}/process`, {
      method: "POST",
    }),

  reindexDocument: (documentId: string) =>
    request<ReindexDocumentResponse>(`/documents/${documentId}/reindex`, {
      method: "POST",
    }),
};
