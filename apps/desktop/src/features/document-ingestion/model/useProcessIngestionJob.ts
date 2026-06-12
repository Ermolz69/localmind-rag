import { useState } from "react";
import type { DocumentSummary } from "@entities/document";
import { documentsApi, ingestionApi } from "@shared/api";
import { useApiMutation } from "@shared/lib/hooks";

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

  const processMutation = useApiMutation(
    async (documentId: string) => {
      await documentsApi.reindexDocument(documentId);
    },
    { fallbackError: "Ingestion failed." },
  );

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
    try {
      const success = await processMutation.mutate(document.id);
      if (success !== null) {
        await refreshAfterProcessing();
      }
    } finally {
      setProcessingDocumentId(null);
    }
  }



  return {
    processDocument,
    processError: processMutation.error,
    processingDocumentId,

  };
}
