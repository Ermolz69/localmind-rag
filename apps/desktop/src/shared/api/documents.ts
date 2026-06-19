import type {
  OperationData,
  OperationPath,
  OperationQuery,
} from "@shared/contracts";
import { toQueryString } from "./common";
import { getApiBaseUrl, request } from "./http";

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

  getDocumentPreview: (documentId: OperationPath<"GetDocumentPreview">["id"]) =>
    request<OperationData<"GetDocumentPreview">>(
      `/documents/${documentId}/preview`,
    ),

  getPreviewFileUrl: (previewUrl: string) => {
    const baseUrl = getApiBaseUrl();
    const path = previewUrl.startsWith("/api/v1/")
      ? previewUrl
      : `/api/v1${previewUrl.startsWith("/") ? previewUrl : `/${previewUrl}`}`;

    return `${baseUrl}${path}`;
  },

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

  reindexDocument: (documentId: OperationPath<"ReindexDocument">["id"]) =>
    request<OperationData<"ReindexDocument">>(
      `/documents/${documentId}/reindex`,
      {
        method: "POST",
      },
    ),
};
