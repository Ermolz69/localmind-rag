import { FileText, Loader2, Play } from "lucide-react";
import type { DocumentSummary } from "@entities/document";
import { documentStatusStyles } from "@shared/constants/ui";
import { Button } from "@shared/ui";
import { EmptyState } from "@shared/ui";
import { InlineError } from "@shared/ui";
import { StatusBadge } from "@shared/ui";

type DocumentListProps = {
  documents: DocumentSummary[];
  isLoading: boolean;
  processingDocumentId: string | null;
  hasMore: boolean;
  isLoadingMore: boolean;
  onProcess: (document: DocumentSummary) => void;
  onLoadMore: () => void;
};

export function DocumentList({
  documents,
  isLoading,
  processingDocumentId,
  hasMore,
  isLoadingMore,
  onProcess,
  onLoadMore,
}: DocumentListProps) {
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
      <div className="overflow-hidden rounded-md border border-border bg-card">
        <div className="grid grid-cols-[minmax(0,1fr)_8rem_10rem_8rem] border-b border-border px-4 py-3 text-xs font-medium uppercase text-muted-foreground">
          <span>Name</span>
          <span>Status</span>
          <span>Created</span>
          <span>Action</span>
        </div>
        {documents.map((document) => {
          const canProcess = ["Queued", "Failed"].includes(document.status);
          const isProcessing = processingDocumentId === document.id;
          return (
            <div
              key={document.id}
              className="grid grid-cols-[minmax(0,1fr)_8rem_10rem_8rem] items-center gap-3 border-b border-border px-4 py-3 last:border-b-0"
            >
              <div className="min-w-0">
                <p className="truncate text-sm font-medium text-card-foreground">
                  {document.name}
                </p>
                <InlineError
                  message={
                    document.status === "Failed"
                      ? (document.lastError ?? "Ingestion failed.")
                      : document.id
                  }
                />
              </div>
              <StatusBadge
                label={document.status}
                className={
                  documentStatusStyles[document.status] ??
                  documentStatusStyles.Queued
                }
              />
              <span className="text-sm text-muted-foreground">
                {new Date(document.createdAt).toLocaleDateString()}
              </span>
              <Button
                variant="secondary"
                disabled={!canProcess || isProcessing}
                onClick={() => onProcess(document)}
              >
                {isProcessing ? (
                  <Loader2 className="animate-spin" size={16} aria-hidden />
                ) : (
                  <Play size={16} aria-hidden />
                )}
                Process
              </Button>
            </div>
          );
        })}
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
