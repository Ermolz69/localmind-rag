import { useState } from "react";
import type { DocumentSummary } from "@entities/document";
import { documentsApi } from "@shared/api";
import { useApiMutation } from "@shared/lib/hooks";

type UseDeleteDocumentOptions = {
  onDeleted: () => Promise<void>;
};

export function useDeleteDocument({ onDeleted }: UseDeleteDocumentOptions) {
  const [deletingDocumentId, setDeletingDocumentId] = useState<string | null>(
    null,
  );

  const deleteMutation = useApiMutation(
    async (documentId: string) => {
      await documentsApi.deleteDocument(documentId);
      // The delete responds with an empty body, so return a non-null sentinel to
      // tell a successful delete apart from a failure (which resolves to null).
      return true;
    },
    { fallbackError: "Could not delete the document." },
  );

  async function deleteDocument(document: DocumentSummary) {
    setDeletingDocumentId(document.id);
    try {
      const result = await deleteMutation.mutate(document.id);
      if (result) {
        await onDeleted();
      }
    } finally {
      setDeletingDocumentId(null);
    }
  }

  return {
    deleteDocument,
    deleteError: deleteMutation.error,
    deletingDocumentId,
  };
}
