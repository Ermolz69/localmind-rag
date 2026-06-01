import type { RagSource, SemanticSearchResponse } from "@entities/source";

import { request } from "./http";

type SemanticSearchOptions = {
  bucketId?: string | null;
  documentId?: string | null;
  limit?: number;
};

export const searchApi = {
  semanticSearch: (query: string, options: SemanticSearchOptions = {}) =>
    request<SemanticSearchResponse>("/search/semantic", {
      method: "POST",
      body: JSON.stringify({
        query,
        limit: options.limit,
        bucketId: options.bucketId,
        documentId: options.documentId,
      }),
    }).then((response): RagSource[] => response.sources),
};
