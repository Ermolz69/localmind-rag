import type { RagSource, SemanticSearchResponse } from "@entities/source";

import { request } from "./http";

type SemanticSearchOptions = {
  bucketId?: string | null;
  documentId?: string | null;
  limit?: number;
  tags?: Record<string, string> | null;
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
        tags: options.tags,
      }),
    }).then((response): RagSource[] => response.sources),
};
