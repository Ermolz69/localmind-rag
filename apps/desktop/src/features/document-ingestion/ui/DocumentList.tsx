import { FileText, Loader2, Play, RotateCcw, X } from "lucide-react";
import { useAutoAnimate } from "@formkit/auto-animate/react";
import type { DocumentSummary } from "@entities/document";
import type { IngestionJobDto } from "@shared/contracts";
import {
  ACTIVE_INGESTION_JOB_STATUSES,
  documentStatusStyles,
} from "@shared/constants/ui";
import { Button, EmptyState, InlineError, StatusBadge } from "@shared/ui";

type DocumentListProps = {
  documents: DocumentSummary[];
  isLoading: boolean;
  processingDocumentId: string | null;
  hasMore: boolean;
  isLoadingMore: boolean;
  jobsByDocumentId?: Record<string, IngestionJobDto>;
  onProcess: (document: DocumentSummary) => void;
  onRetry?: (jobId: string) => void;
  onCancel?: (jobId: string) => void;
  onLoadMore: () => void;
};

function DocumentListItem({
  document,
  job,
  isProcessing,
  onRetry,
  onCancel,
  onProcess,
}: {
  document: DocumentSummary;
  job?: IngestionJobDto;
  isProcessing: boolean;
  onRetry?: (jobId: string) => void;
  onCancel?: (jobId: string) => void;
  onProcess: (document: DocumentSummary) => void;
}) {
  const [infoRef] = useAutoAnimate<HTMLDivElement>();
  const [actionsRef] = useAutoAnimate<HTMLDivElement>();

  const canProcess = ["Queued", "Failed"].includes(document.status) && !job;

  return (
    <div className="group grid grid-cols-[minmax(0,1fr)_8rem_10rem_auto] items-start gap-3 border-b border-border px-4 py-3 transition-colors duration-200 hover:bg-muted/50 last:border-b-0">
      <div ref={infoRef} className="min-w-0 mt-2">
        <p className="truncate text-sm font-medium text-card-foreground">
          {document.name}
        </p>

        {job && ACTIVE_INGESTION_JOB_STATUSES.has(job.status) ? (
          <div className="mt-1 flex items-center gap-2 text-xs text-muted-foreground">
            <span className="truncate">{job.currentStep}</span>
            <span className="font-mono">{job.progressPercent}%</span>
          </div>
        ) : (
          <InlineError
            message={
              document.status === "Failed"
                ? (document.lastError ?? job?.errorMessage ?? "Ingestion failed.")
                : document.id
            }
          />
        )}

        {job && Number(job.retryCount) > 0 ? (
          <p className="mt-1 text-xs text-muted-foreground">
            Retries: {job.retryCount}
          </p>
        ) : null}
      </div>
      <StatusBadge
        label={job?.status ?? document.status}
        className={`w-max mt-[7px] ${
          documentStatusStyles[document.status] ?? documentStatusStyles.Queued
        }`}
      />
      <span className="mt-2 text-sm text-muted-foreground">
        {new Date(document.createdAt).toLocaleDateString()}
      </span>
      <div ref={actionsRef} className="flex justify-end gap-2">
        {job?.canRetry && onRetry ? (
          <Button
            variant="secondary"
            onClick={() => onRetry(job.id)}
            disabled={isProcessing}
          >
            {isProcessing ? (
              <Loader2 className="animate-spin" size={16} aria-hidden />
            ) : (
              <RotateCcw size={16} aria-hidden />
            )}
            Retry
          </Button>
        ) : null}
        {job?.canCancel && onCancel ? (
          <Button variant="secondary" onClick={() => onCancel(job.id)}>
            <X size={16} aria-hidden />
            Cancel
          </Button>
        ) : null}
        {canProcess ? (
          <Button
            variant="secondary"
            disabled={isProcessing}
            onClick={() => onProcess(document)}
          >
            {isProcessing ? (
              <Loader2 className="animate-spin" size={16} aria-hidden />
            ) : (
              <Play size={16} aria-hidden />
            )}
            Process
          </Button>
        ) : null}
      </div>
    </div>
  );
}

export function DocumentList({
  documents,
  isLoading,
  processingDocumentId,
  hasMore,
  isLoadingMore,
  jobsByDocumentId = {},
  onProcess,
  onRetry,
  onCancel,
  onLoadMore,
}: DocumentListProps) {
  const [listRef] = useAutoAnimate<HTMLDivElement>();

  if (isLoading) {
    return (
      <div className="rounded-md border border-border bg-card p-6 text-sm text-muted-foreground">
        Loading documents...
      </div>
    );
  }

  if (documents.length === 0) {
    return (
      <EmptyState
        icon={<FileText size={18} aria-hidden />}
        title="No documents here yet"
        description="Upload a local file to create a queued ingestion job."
      />
    );
  }

  return (
    <div className="space-y-3">
      <div ref={listRef} className="overflow-hidden rounded-md border border-border bg-card">
        <div className="grid grid-cols-[minmax(0,1fr)_8rem_10rem_auto] gap-3 border-b border-border px-4 py-3 text-xs font-medium uppercase text-muted-foreground">
          <span>Name</span>
          <span>Status</span>
          <span>Created</span>
          <span className="justify-self-end">Action</span>
        </div>
        {documents.map((document) => (
          <DocumentListItem
            key={document.id}
            document={document}
            job={jobsByDocumentId[document.id]}
            isProcessing={processingDocumentId === document.id}
            onRetry={onRetry}
            onCancel={onCancel}
            onProcess={onProcess}
          />
        ))}
      </div>
      {hasMore ? (
        <Button
          className="w-full"
          variant="secondary"
          disabled={isLoadingMore}
          onClick={onLoadMore}
        >
          {isLoadingMore ? "Loading..." : "Load more"}
        </Button>
      ) : null}
    </div>
  );
}
