import type { Schema } from "@shared/contracts";

type DocumentSummary = Schema<"DocumentDto">;

export function createDocumentSummary(
  overrides?: Partial<DocumentSummary>,
): DocumentSummary {
  return {
    id: crypto.randomUUID(),
    bucketId: null,
    name: "fixture-document.txt",
    status: "Uploaded",
    createdAt: new Date().toISOString(),
    lastError: null,
    tags: null,
    ...overrides,
  };
}
