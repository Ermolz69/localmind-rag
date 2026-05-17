import { useCallback, useEffect, useMemo, useState } from "react";
import type { BucketDto } from "@entities/bucket";
import type { DocumentSummary } from "@entities/document";
import { bucketsApi, documentsApi, getErrorMessage } from "@shared/api";
import { useCursorPage } from "@shared/lib/hooks";

export function useDocumentList() {
  const [buckets, setBuckets] = useState<BucketDto[]>([]);
  const [selectedBucketId, setSelectedBucketId] = useState("");
  const [selectedStatus, setSelectedStatus] = useState("");
  const [newBucketName, setNewBucketName] = useState("");
  const [documentListError, setDocumentListError] = useState<string | null>(
    null,
  );

  const selectedBucketName = useMemo(() => {
    if (!selectedBucketId) {
      return "All buckets";
    }

    return (
      buckets.find((bucket) => bucket.id === selectedBucketId)?.name ??
      "Selected bucket"
    );
  }, [buckets, selectedBucketId]);

  const loadDocumentsPage = useCallback(
    (cursor?: string | null) =>
      documentsApi.getDocuments({
        bucketId: selectedBucketId || null,
        status: selectedStatus || null,
        cursor,
        limit: 30,
      }),
    [selectedBucketId, selectedStatus],
  );

  const documentsPage = useCursorPage<DocumentSummary>(
    loadDocumentsPage,
    "Unable to load documents.",
  );

  const loadBuckets = useCallback(async () => {
    const nextBuckets = await bucketsApi.getBuckets();
    setBuckets(nextBuckets);
  }, []);

  useEffect(() => {
    void loadBuckets();
  }, [loadBuckets]);

  async function createBucket() {
    const name = newBucketName.trim();
    if (!name) {
      return;
    }

    setDocumentListError(null);
    try {
      const bucket = await bucketsApi.createBucket({ name });
      setNewBucketName("");
      setSelectedBucketId(bucket.id);
      await loadBuckets();
    } catch (exception) {
      setDocumentListError(
        getErrorMessage(exception, "Bucket creation failed."),
      );
    }
  }

  return {
    buckets,
    createBucket,
    documentListError: documentListError ?? documentsPage.error,
    documents: documentsPage.items,
    hasMore: documentsPage.hasMore,
    isLoading: documentsPage.isLoading,
    isLoadingMore: documentsPage.isLoadingMore,
    loadBuckets,
    loadMore: documentsPage.loadMore,
    newBucketName,
    reloadDocuments: documentsPage.reload,
    selectedBucketId,
    selectedBucketName,
    selectedStatus,
    setDocumentListError,
    setNewBucketName,
    setSelectedBucketId,
    setSelectedStatus,
  };
}
