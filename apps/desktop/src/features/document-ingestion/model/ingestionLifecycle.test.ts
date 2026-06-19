import { describe, expect, it } from "vitest";
import type { DocumentSummary } from "@entities/document";
import type { IngestionJobDto } from "@shared/contracts";

import {
  filterDocumentsByLifecycleStatus,
  getDocumentLifecycleStatus,
  getDocumentStatusQuery,
} from "./ingestionLifecycle";

function document(id: string, status: string): DocumentSummary {
  return {
    id,
    bucketId: null,
    name: `${id}.txt`,
    status,
    createdAt: "2026-06-12T00:00:00Z",
    lastError: null,
    tags: null,
  };
}

function job(documentId: string, status: string): IngestionJobDto {
  return {
    id: `${documentId}-job`,
    documentId,
    status,
    progressPercent: 75,
    currentStep: status,
    createdAt: "2026-06-12T00:00:00Z",
    updatedAt: null,
    processedAt: null,
    errorCode: null,
    errorMessage: null,
    retryCount: 0,
    canRetry: status === "Failed",
    canCancel: status === "Processing",
    lastOperationId: null,
  };
}

describe("ingestion lifecycle helpers", () => {
  it("maps UI lifecycle filters to document API filters", () => {
    expect(getDocumentStatusQuery("Pending")).toBe("Queued");
    expect(getDocumentStatusQuery("Chunking")).toBe("Processing");
    expect(getDocumentStatusQuery("Embedding")).toBe("Processing");
    expect(getDocumentStatusQuery("Cancelled")).toBe("Queued");
  });

  it("uses ingestion job status as the visible lifecycle state", () => {
    expect(
      getDocumentLifecycleStatus(
        document("doc", "Processing"),
        job("doc", "Chunking"),
      ),
    ).toBe("Chunking");
  });

  it("shows queued documents as pending when no job is loaded", () => {
    expect(getDocumentLifecycleStatus(document("doc", "Queued"))).toBe(
      "Pending",
    );
  });

  it("filters by job-only lifecycle states", () => {
    const documents = [
      document("a", "Processing"),
      document("b", "Processing"),
    ];
    const jobsByDocumentId = {
      a: job("a", "Chunking"),
      b: job("b", "Embedding"),
    };

    expect(
      filterDocumentsByLifecycleStatus(
        documents,
        jobsByDocumentId,
        "Embedding",
      ),
    ).toEqual([documents[1]]);
  });
});
