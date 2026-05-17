import { Loader2, RefreshCw, Upload } from "lucide-react";
import {
  DocumentList,
  QueuedIngestionNotice,
} from "@features/document-ingestion";
import { DocumentDropzone } from "@features/document-upload";
import { BucketPanel } from "@features/bucket-management";
import { RuntimePanel } from "@features/settings";
import { Button, ErrorBanner, PageHeader, Select, Toolbar } from "@shared/ui";
import { useDocumentsPageViewModel } from "./model/useDocumentsPageViewModel";

const documentStatuses = ["Queued", "Processing", "Indexed", "Failed"];

export function DocumentsPage() {
  const page = useDocumentsPageViewModel();

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

      <RuntimePanel
        health={page.health}
        runtime={page.runtime}
        sync={page.sync}
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
              onChange={(event) => page.setSelectedBucketId(event.target.value)}
            >
              <option value="">All buckets</option>
              {page.buckets.map((bucket) => (
                <option key={bucket.id} value={bucket.id}>
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
              onChange={(event) => page.setSelectedStatus(event.target.value)}
            >
              <option value="">All statuses</option>
              {documentStatuses.map((status) => (
                <option key={status} value={status}>
                  {status}
                </option>
              ))}
            </Select>
            <span className="text-sm text-muted-foreground">
              {page.selectedBucketName}
            </span>
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
              onProcess={() => void page.processLastUpload()}
            />
          ) : null}

          <ErrorBanner message={page.error} />

          <DocumentList
            documents={page.documents}
            isLoading={page.isLoading}
            processingDocumentId={page.processingDocumentId}
            hasMore={page.hasMore}
            isLoadingMore={page.isLoadingMore}
            onProcess={(document) => void page.processDocument(document)}
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
    </section>
  );
}
