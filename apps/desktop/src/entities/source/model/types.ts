export type RagSource = {
  documentId: string;
  documentName: string;
  chunkId: string;
  pageNumber: number | null;
  score: number;
  snippet: string;
};

export type SemanticSearchResponse = {
  sources: RagSource[];
};
