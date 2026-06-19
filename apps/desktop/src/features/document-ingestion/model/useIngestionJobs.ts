import { useEffect, useMemo, useRef, useState } from "react";
import type { IngestionJobDto } from "@shared/contracts";
import { ingestionApi } from "@shared/api";
import { ACTIVE_INGESTION_JOB_STATUSES } from "@shared/constants/ui";
import { useApiMutation } from "@shared/lib/hooks";

export function useIngestionJobs(
  shouldPoll: boolean,
  onActiveJobsFinished?: () => void,
) {
  const [jobs, setJobs] = useState<IngestionJobDto[]>([]);
  const [hasActiveJobs, setHasActiveJobs] = useState(false);
  const previousHasActiveJobs = useRef(false);

  useEffect(() => {
    if (previousHasActiveJobs.current && !hasActiveJobs) {
      onActiveJobsFinished?.();
    }
    previousHasActiveJobs.current = hasActiveJobs;
  }, [hasActiveJobs, onActiveJobsFinished]);

  useEffect(() => {
    let timeoutId: number;
    let isCancelled = false;

    async function loadJobs() {
      const response = await ingestionApi.getJobs({ limit: 100, offset: 0 });
      if (isCancelled) return false;

      const fetchedJobs = response.items;
      setJobs(fetchedJobs);

      const active = fetchedJobs.some((job) =>
        ACTIVE_INGESTION_JOB_STATUSES.has(job.status),
      );
      setHasActiveJobs(active);
      return active;
    }

    async function poll() {
      try {
        const active = await loadJobs();
        if (!isCancelled && active) {
          timeoutId = window.setTimeout(poll, 3000);
        }
      } catch {
        // Ignore polling errors to prevent breaking the loop aggressively
      }
    }

    if (shouldPoll || hasActiveJobs) {
      void poll();
    }

    return () => {
      isCancelled = true;
      window.clearTimeout(timeoutId);
    };
  }, [shouldPoll, hasActiveJobs]);

  const jobsByDocumentId = useMemo(() => {
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
  }, [jobs]);

  async function refreshJobs() {
    const response = await ingestionApi.getJobs({ limit: 100, offset: 0 });
    const fetchedJobs = response.items;
    setJobs(fetchedJobs);
    setHasActiveJobs(
      fetchedJobs.some((job) => ACTIVE_INGESTION_JOB_STATUSES.has(job.status)),
    );
  }

  const retryMutation = useApiMutation(
    async (jobId: string) => {
      await ingestionApi.retryJob(jobId);
      await ingestionApi.processJob(jobId);
      await refreshJobs();
    },
    { fallbackError: "Failed to retry ingestion job." },
  );

  const cancelMutation = useApiMutation(
    async (jobId: string) => {
      await ingestionApi.cancelJob(jobId);
      await refreshJobs();
    },
    { fallbackError: "Failed to cancel ingestion job." },
  );

  return {
    jobs,
    jobsByDocumentId,
    retryJob: retryMutation.mutate,
    retryError: retryMutation.error,
    cancelJob: cancelMutation.mutate,
    cancelError: cancelMutation.error,
  };
}
