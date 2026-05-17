import { useCallback, useMemo, useState } from "react";
import type { BucketDto } from "@entities/bucket";
import type { DocumentSummary } from "@entities/document";
import { bucketsApi, documentsApi, getErrorMessage } from "@shared/api";
import { useCursorPage, useDebouncedValue } from "@shared/lib/hooks";

export function useDocumentList() {
  const [selectedBucketId, setSelectedBucketId] = useState("");
  const [selectedStatus, setSelectedStatus] = useState("");
  const [newBucketName, setNewBucketName] = useState("");
  const [bucketQuery, setBucketQuery] = useState("");
  const debouncedBucketQuery = useDebouncedValue(bucketQuery, 250);
  const [documentListError, setDocumentListError] = useState<string | null>(
    null,
  );

  const loadBucketsPage = useCallback(
    (cursor?: string | null) =>
      bucketsApi.getBucketsPage({
        query: debouncedBucketQuery || null,
        cursor,
        limit: 24,
      }),
    [debouncedBucketQuery],
  );

  const bucketsPage = useCursorPage<BucketDto>(
    loadBucketsPage,
    "Unable to load buckets.",
  );

  const selectedBucketName = useMemo(() => {
    if (!selectedBucketId) {
      return "All buckets";
    }

    return (
      bucketsPage.items.find((bucket) => bucket.id === selectedBucketId)
        ?.name ?? "Selected bucket"
    );
  }, [bucketsPage.items, selectedBucketId]);

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
      await bucketsPage.reload();
    } catch (exception) {
      setDocumentListError(
        getErrorMessage(exception, "Bucket creation failed."),
      );
    }
  }

  return {
    bucketQuery,
    buckets: bucketsPage.items,
    bucketsHasMore: bucketsPage.hasMore,
    bucketsIsLoading: bucketsPage.isLoading,
    bucketsIsLoadingMore: bucketsPage.isLoadingMore,
    createBucket,
    documentListError:
      documentListError ?? documentsPage.error ?? bucketsPage.error,
    documents: documentsPage.items,
    hasMore: documentsPage.hasMore,
    isLoading: documentsPage.isLoading,
    isLoadingMore: documentsPage.isLoadingMore,
    loadBuckets: bucketsPage.reload,
    loadMoreBuckets: bucketsPage.loadMore,
    loadMore: documentsPage.loadMore,
    newBucketName,
    reloadDocuments: documentsPage.reload,
    selectedBucketId,
    selectedBucketName,
    selectedStatus,
    setDocumentListError,
    setBucketQuery,
    setNewBucketName,
    setSelectedBucketId,
    setSelectedStatus,
  };
}
