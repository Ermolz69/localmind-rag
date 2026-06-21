import { Loader2, RefreshCw, Upload } from "lucide-react";
import {
  DocumentList,
  INGESTION_LIFECYCLE_STATUSES,
  QueuedIngestionNotice,
} from "@features/document-ingestion";
import {
  DocumentPreviewModal,
  useDocumentPreview,
} from "@features/document-preview";
import { DocumentDropzone } from "@features/document-upload";
import { BucketPanel } from "@features/bucket-management";
import {
  Button,
  ErrorBanner,
  PageHeader,
  Select,
  Toolbar,
  Tooltip,
} from "@shared/ui";
import { useDocumentsPageViewModel } from "./model/useDocumentsPageViewModel";

export function DocumentsPage() {
  const page = useDocumentsPageViewModel();
  const preview = useDocumentPreview();

  return (
    <section className="space-y-5">
      <PageHeader
        title="Documents"
        description="Local files, bucket routing, ingestion status, and runtime readiness."
        actions={
          <>
            <Button variant="secondary" onClick={() => void page.reload()}>
              <RefreshCw size={16} aria-hidden />
              Refresh
            </Button>
            <Button
              onClick={() => page.fileInputRef.current?.click()}
              disabled={page.isUploading}
            >
              {page.isUploading ? (
                <Loader2 className="animate-spin" size={16} aria-hidden />
              ) : (
                <Upload size={16} aria-hidden />
              )}
              Upload
            </Button>
          </>
        }
      />

      <div className="grid gap-4 lg:grid-cols-[minmax(0,1fr)_20rem]">
        <div className="space-y-4">
          <Toolbar>
            <label className="text-sm font-medium" htmlFor="bucket-filter">
              Bucket
            </label>
            <Select
              id="bucket-filter"
              className="max-w-56"
              value={page.selectedBucketId}
              title={page.selectedBucketName || "All buckets"}
              onChange={(event) => page.setSelectedBucketId(event.target.value)}
            >
              <option value="" title="All buckets">
                All buckets
              </option>
              {page.buckets.map((bucket) => (
                <option key={bucket.id} value={bucket.id} title={bucket.name}>
                  {bucket.name}
                </option>
              ))}
            </Select>
            <label className="text-sm font-medium" htmlFor="status-filter">
              Status
            </label>
            <Select
              id="status-filter"
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
            <Tooltip
              content={page.selectedBucketName}
              className="flex min-w-0 flex-1"
            >
              <span className="block w-full truncate text-sm text-muted-foreground">
                {page.selectedBucketName}
              </span>
            </Tooltip>
          </Toolbar>

          <DocumentDropzone
            fileInputRef={page.fileInputRef}
            isDragging={page.isDragging}
            onDraggingChange={page.setIsDragging}
            onFileSelected={(file) => void page.uploadFile(file)}
          />

          {page.lastUpload ? (
            <QueuedIngestionNotice
              fileName={page.lastUpload.fileName}
              isProcessing={
                page.processingDocumentId === page.lastUpload.documentId
              }
              job={page.jobsByDocumentId[page.lastUpload.documentId]}
              onCancel={
                page.jobsByDocumentId[page.lastUpload.documentId]?.canCancel
                  ? () =>
                      void page.cancelJob(
                        page.jobsByDocumentId[page.lastUpload!.documentId].id,
                      )
                  : undefined
              }
            />
          ) : null}

          <ErrorBanner message={page.error} />

          <DocumentList
            documents={page.filteredDocuments}
            isLoading={page.isLoading}
            processingDocumentId={page.processingDocumentId}
            hasMore={page.hasMore}
            isLoadingMore={page.isLoadingMore}
            jobsByDocumentId={page.jobsByDocumentId}
            onProcess={(document) => void page.processDocument(document)}
            onPreview={(document) => void preview.openPreview(document)}
            onRetry={(jobId) => void page.retryJob(jobId)}
            onCancel={(jobId) => void page.cancelJob(jobId)}
            onDelete={(document) => void page.deleteDocument(document)}
            deletingDocumentId={page.deletingDocumentId}
            onLoadMore={() => void page.loadMore()}
          />
        </div>

        <BucketPanel
          buckets={page.buckets}
          bucketQuery={page.bucketQuery}
          hasMore={page.bucketsHasMore}
          isLoading={page.bucketsIsLoading}
          isLoadingMore={page.bucketsIsLoadingMore}
          newBucketName={page.newBucketName}
          selectedBucketId={page.selectedBucketId}
          onBucketNameChange={page.setNewBucketName}
          onCreateBucket={() => void page.createBucket()}
          onLoadMore={() => void page.loadMoreBuckets()}
          onQueryChange={page.setBucketQuery}
          onSelectBucket={page.setSelectedBucketId}
        />
      </div>

      <DocumentPreviewModal
        document={preview.document}
        error={preview.error}
        isLoading={preview.isLoading}
        open={preview.isOpen}
        preview={preview.preview}
        onClose={preview.closePreview}
      />
    </section>
  );
}
