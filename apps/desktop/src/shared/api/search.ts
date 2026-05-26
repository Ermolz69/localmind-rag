import type { RagSource, SemanticSearchResponse } from "@entities/source";

import { request } from "./http";

export const searchApi = {
  semanticSearch: (query: string) =>
    request<SemanticSearchResponse>("/search/semantic", {
      method: "POST",
      body: JSON.stringify({ query }),
    }).then((response): RagSource[] => response.sources),
};
