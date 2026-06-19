import type { DocumentSummary } from "@entities/document";
import type { IngestionJobDto } from "@shared/contracts";

export const INGESTION_LIFECYCLE_STATUSES = [
  "Pending",
  "Processing",
  "Chunking",
  "Embedding",
  "Indexed",
  "Failed",
  "Cancelled",
] as const;

export type IngestionLifecycleStatus =
  (typeof INGESTION_LIFECYCLE_STATUSES)[number];

const documentStatusByLifecycleStatus: Partial<
  Record<IngestionLifecycleStatus, string>
> = {
  Pending: "Queued",
  Processing: "Processing",
  Chunking: "Processing",
  Embedding: "Processing",
  Indexed: "Indexed",
  Failed: "Failed",
  Cancelled: "Queued",
};

export function getDocumentStatusQuery(status: string): string | undefined {
  if (!status) {
    return undefined;
  }

  return documentStatusByLifecycleStatus[status as IngestionLifecycleStatus];
}

export function getDocumentLifecycleStatus(
  document: DocumentSummary,
  job?: IngestionJobDto,
): string {
  if (job) {
    return job.status;
  }

  return document.status === "Queued" ? "Pending" : document.status;
}

export function filterDocumentsByLifecycleStatus(
  documents: DocumentSummary[],
  jobsByDocumentId: Record<string, IngestionJobDto>,
  status: string,
): DocumentSummary[] {
  if (!status) {
    return documents;
  }

  return documents.filter(
    (document) =>
      getDocumentLifecycleStatus(document, jobsByDocumentId[document.id]) ===
      status,
  );
}
