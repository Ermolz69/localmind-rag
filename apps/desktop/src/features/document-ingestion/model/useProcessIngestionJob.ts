import { useState } from "react";
import type { DocumentSummary } from "@entities/document";
import { documentsApi, getErrorMessage } from "@shared/api";

const refreshAttempts = 4;

type UseProcessIngestionJobOptions = {
  onProcessed: () => Promise<void>;
};

export function useProcessIngestionJob({
  onProcessed,
}: UseProcessIngestionJobOptions) {
  const [processingDocumentId, setProcessingDocumentId] = useState<
    string | null
  >(null);
  const [processError, setProcessError] = useState<string | null>(null);

  async function refreshAfterProcessing() {
    for (let attempt = 0; attempt < refreshAttempts; attempt += 1) {
      await onProcessed();
      await new Promise((resolve) => window.setTimeout(resolve, 700));
    }
  }

  async function processDocument(document: DocumentSummary) {
    if (!["Queued", "Failed"].includes(document.status)) {
      return;
    }

    setProcessingDocumentId(document.id);
    setProcessError(null);
    try {
      const reindex = await documentsApi.reindexDocument(document.id);
      await documentsApi.processIngestionJob(reindex.ingestionJobId);
      await refreshAfterProcessing();
    } catch (exception) {
      setProcessError(getErrorMessage(exception, "Ingestion failed."));
    } finally {
      setProcessingDocumentId(null);
    }
  }

  async function processUploadedDocument(
    documentId: string,
    ingestionJobId: string,
    onComplete: () => void,
  ) {
    setProcessingDocumentId(documentId);
    setProcessError(null);
    try {
      await documentsApi.processIngestionJob(ingestionJobId);
      onComplete();
      await refreshAfterProcessing();
    } catch (exception) {
      setProcessError(getErrorMessage(exception, "Ingestion failed."));
    } finally {
      setProcessingDocumentId(null);
    }
  }

  return {
    processDocument,
    processError,
    processingDocumentId,
    processUploadedDocument,
  };
}
