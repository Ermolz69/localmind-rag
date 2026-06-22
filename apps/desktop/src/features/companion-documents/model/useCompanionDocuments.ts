import { useCallback, useEffect, useRef, useState } from "react";

import type { DocumentSummary } from "@entities/document";
import type { IngestionJobDto } from "@shared/contracts";
import { documentsApi, getErrorMessage, ingestionApi } from "@shared/api";

export type CompanionDocument = {
  id: string;
  name: string;
  status: string;
  progressPercent: number | null;
  errorMessage: string | null;
};

/** Lifecycle statuses where a document is still being processed. */
export const ACTIVE_DOCUMENT_STATUSES = new Set([
  "Pending",
  "Processing",
  "Chunking",
  "Embedding",
]);

const POLL_INTERVAL_MS = 3000;

// Mirrors the desktop getDocumentLifecycleStatus: prefer the live job status,
// otherwise the document's own status (Queued surfaces as Pending).
function lifecycleStatus(
  document: DocumentSummary,
  job: IngestionJobDto | undefined,
): string {
  if (job) {
    return job.status;
  }

  return document.status === "Queued" ? "Pending" : document.status;
}

function buildJobsByDocumentId(
  jobs: IngestionJobDto[],
): Record<string, IngestionJobDto> {
  const map: Record<string, IngestionJobDto> = {};

  for (const job of jobs) {
    const existing = map[job.documentId];

    if (!existing) {
      map[job.documentId] = job;
      continue;
    }

    const existingTime = new Date(
      existing.updatedAt ?? existing.createdAt,
    ).getTime();
    const jobTime = new Date(job.updatedAt ?? job.createdAt).getTime();

    if (jobTime > existingTime) {
      map[job.documentId] = job;
    }
  }

  return map;
}

/**
 * Read-only view of the computer's documents and their indexing state for the
 * phone: status, progress, and failure reasons. Polls while work is in flight.
 */
export function useCompanionDocuments() {
  const [documents, setDocuments] = useState<CompanionDocument[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const activeRef = useRef(false);

  const load = useCallback(async () => {
    try {
      const [documentsPage, jobsPage] = await Promise.all([
        documentsApi.getDocuments({ limit: 100 }),
        ingestionApi.getJobs({ limit: 100, offset: 0 }),
      ]);

      const jobsByDocumentId = buildJobsByDocumentId(jobsPage.items);

      const mapped = documentsPage.items.map<CompanionDocument>((document) => {
        const job = jobsByDocumentId[document.id];
        const status = lifecycleStatus(document, job);

        return {
          id: document.id,
          name: document.name,
          status,
          progressPercent: job ? Number(job.progressPercent) : null,
          errorMessage: job?.errorMessage ?? document.lastError ?? null,
        };
      });

      setDocuments(mapped);
      setError(null);
      activeRef.current = mapped.some((document) =>
        ACTIVE_DOCUMENT_STATUSES.has(document.status),
      );
    } catch (exception) {
      setError(getErrorMessage(exception, "Unable to load documents."));
      activeRef.current = false;
    } finally {
      setIsLoading(false);
    }
  }, []);

  // Initial load, then poll only while indexing work is in flight.
  useEffect(() => {
    let cancelled = false;
    let timeout: number | undefined;

    async function tick() {
      await load();
      if (cancelled) {
        return;
      }
      if (activeRef.current) {
        timeout = window.setTimeout(tick, POLL_INTERVAL_MS);
      }
    }

    void tick();

    return () => {
      cancelled = true;
      if (timeout) {
        window.clearTimeout(timeout);
      }
    };
  }, [load]);

  return { documents, isLoading, error, refresh: load };
}
