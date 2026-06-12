import type { OperationData, OperationJsonBody } from "@shared/contracts";

import { request } from "./http";

type SemanticSearchOptions = Partial<
  Omit<OperationJsonBody<"SemanticSearch">, "query">
>;

type ContentSearchOptions = Partial<
  Omit<OperationJsonBody<"ContentSearch">, "query">
>;

export const searchApi = {
  semanticSearch: (query: string, options: SemanticSearchOptions = {}) =>
    request<OperationData<"SemanticSearch">>("/search/semantic", {
      method: "POST",
      body: JSON.stringify({
        query,
        limit: options.limit ?? 8,
        bucketId: options.bucketId,
        documentId: options.documentId,
        tags: options.tags,
        dateFrom: options.dateFrom,
        dateTo: options.dateTo,
        fileType: options.fileType,
      }),
    }).then((response) => response.sources),

  contentSearch: (query: string, options: ContentSearchOptions = {}) =>
    request<OperationData<"ContentSearch">>("/search/content", {
      method: "POST",
      body: JSON.stringify({
        query,
        limit: options.limit ?? 20,
        bucketId: options.bucketId,
        documentId: options.documentId,
        tags: options.tags,
        dateFrom: options.dateFrom,
        dateTo: options.dateTo,
        fileType: options.fileType,
        includeDocuments: options.includeDocuments ?? true,
        includeNotes: options.includeNotes ?? true,
      }),
    }).then((response) => response.results),
};
