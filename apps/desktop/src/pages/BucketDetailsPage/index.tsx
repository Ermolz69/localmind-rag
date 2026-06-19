import { ArrowLeft, RefreshCw } from "lucide-react";
import { useMemo } from "react";
import { useNavigate, useParams } from "react-router-dom";
import {
  DocumentList,
  INGESTION_LIFECYCLE_STATUSES,
} from "@features/document-ingestion";
import { routes } from "@shared/constants/routes";
import {
  Button,
  EmptyState,
  ErrorBanner,
  PageHeader,
  Select,
  Toolbar,
  Tooltip,
} from "@shared/ui";
import { useDocumentsPageViewModel } from "@pages/DocumentsPage/model/useDocumentsPageViewModel";

export function BucketDetailsPage() {
  const { bucketId = "" } = useParams();
  const navigate = useNavigate();
  const page = useDocumentsPageViewModel({
    selectedBucketId: bucketId,
    onSelectedBucketIdChange: (nextBucketId) => {
      if (nextBucketId) {
        navigate(routes.bucketDetails(nextBucketId));
      }
    },
  });

  const currentBucket = useMemo(
    () => page.buckets.find((bucket) => bucket.id === bucketId),
    [bucketId, page.buckets],
  );
  const isMissingBucket =
    bucketId !== "" && !page.bucketsIsLoading && !currentBucket;
  const title = currentBucket?.name ?? "Bucket details";

  if (!bucketId || isMissingBucket) {
    return (
      <section className="space-y-5">
        <PageHeader
          title="Bucket not found"
          description="This bucket may have been deleted or is unavailable."
          actions={
            <Button
              variant="secondary"
              onClick={() => navigate(routes.buckets)}
            >
              <ArrowLeft size={16} aria-hidden />
              Back to buckets
            </Button>
          }
        />
        <EmptyState
          title="Bucket not found"
          description="Return to the bucket list and choose an available bucket."
        />
      </section>
    );
  }

  return (
    <section className="space-y-5">
      <PageHeader
        title={title}
        description="Documents stored in this local bucket."
        actions={
          <>
            <Button
              variant="secondary"
              onClick={() => navigate(routes.buckets)}
            >
              <ArrowLeft size={16} aria-hidden />
              Back
            </Button>
            <Button variant="secondary" onClick={() => void page.reload()}>
              <RefreshCw size={16} aria-hidden />
              Refresh
            </Button>
          </>
        }
      />

      <Toolbar>
        <label className="text-sm font-medium" htmlFor="bucket-route-filter">
          Bucket
        </label>
        <Select
          id="bucket-route-filter"
          className="max-w-64"
          value={bucketId}
          title={currentBucket?.name ?? "Loading buckets..."}
          onChange={(event) => page.setSelectedBucketId(event.target.value)}
          disabled={page.bucketsIsLoading}
        >
          {page.bucketsIsLoading ? (
            <option value={bucketId} title="Loading buckets...">
              Loading buckets...
            </option>
          ) : null}
          {page.buckets.map((bucket) => (
            <option key={bucket.id} value={bucket.id} title={bucket.name}>
              {bucket.name}
            </option>
          ))}
        </Select>
        <label className="text-sm font-medium" htmlFor="bucket-status-filter">
          Status
        </label>
        <Select
          id="bucket-status-filter"
          className="max-w-44"
          value={page.selectedStatus}
          title={page.selectedStatus || "All statuses"}
          onChange={(event) => page.setSelectedStatus(event.target.value)}
        >
          <option value="" title="All statuses">
            All statuses
          </option>
          {INGESTION_LIFECYCLE_STATUSES.map((status) => (
            <option key={status} value={status} title={status}>
              {status}
            </option>
          ))}
        </Select>
        <Tooltip content={title} className="flex min-w-0 flex-1">
          <span className="block w-full truncate text-sm text-muted-foreground">
            {title}
          </span>
        </Tooltip>
      </Toolbar>

      <ErrorBanner message={page.error} />

      <DocumentList
        documents={page.filteredDocuments}
        isLoading={page.isLoading}
        processingDocumentId={page.processingDocumentId}
        hasMore={page.hasMore}
        isLoadingMore={page.isLoadingMore}
        jobsByDocumentId={page.jobsByDocumentId}
        onProcess={(document) => void page.processDocument(document)}
        onRetry={(jobId) => void page.retryJob(jobId)}
        onCancel={(jobId) => void page.cancelJob(jobId)}
        onLoadMore={() => void page.loadMore()}
      />
    </section>
  );
}
