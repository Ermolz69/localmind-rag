import { useCallback, useRef, useState } from "react";
import type { DocumentSummary } from "@entities/document";
import { documentsApi, getErrorMessage } from "@shared/api";
import type { OperationData } from "@shared/contracts";

type DocumentPreviewData = OperationData<"GetDocumentPreview">;

export function useDocumentPreview() {
  const [document, setDocument] = useState<DocumentSummary | null>(null);
  const [preview, setPreview] = useState<DocumentPreviewData | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const requestIdRef = useRef(0);

  const closePreview = useCallback(() => {
    requestIdRef.current += 1;
    setDocument(null);
    setPreview(null);
    setError(null);
    setIsLoading(false);
  }, []);

  const openPreview = useCallback(async (nextDocument: DocumentSummary) => {
    const requestId = requestIdRef.current + 1;
    requestIdRef.current = requestId;
    setDocument(nextDocument);
    setPreview(null);
    setError(null);
    setIsLoading(true);

    try {
      const nextPreview = await documentsApi.getDocumentPreview(
        nextDocument.id,
      );
      if (requestId !== requestIdRef.current) {
        return;
      }

      setPreview(nextPreview);
    } catch (exception) {
      if (requestId !== requestIdRef.current) {
        return;
      }

      setError(getErrorMessage(exception, "Unable to load preview."));
    } finally {
      if (requestId === requestIdRef.current) {
        setIsLoading(false);
      }
    }
  }, []);

  return {
    closePreview,
    document,
    error,
    isLoading,
    isOpen: document !== null,
    openPreview,
    preview,
  };
}
