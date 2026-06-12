import type { OperationData, OperationJsonBody } from "@shared/contracts";

import { request } from "./http";

type SemanticSearchOptions = Partial<
  Omit<OperationJsonBody<"SemanticSearch">, "query">
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
};
