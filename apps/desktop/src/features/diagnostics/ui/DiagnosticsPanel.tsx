import { useDiagnostics } from "../model/useDiagnostics";
import { Skeleton } from "@shared/ui";

export function DiagnosticsPanel() {
  const { diagnostics, isLoading, isRefreshing, lastUpdatedAt, error } =
    useDiagnostics();

  if (isLoading) {
    return <DiagnosticsPanelSkeleton />;
  }

  if (error || !diagnostics) {
    return null;
  }

  return (
    <section
      id="diagnostics"
      className="scroll-mt-6 space-y-5 rounded-xl border border-border bg-card p-5 shadow-sm sm:p-6"
    >
      <div>
        <h2 className="text-base font-semibold text-card-foreground">
          Diagnostics
        </h2>
        <div className="mt-1 flex flex-wrap items-center gap-x-3 gap-y-1 text-sm text-muted-foreground">
          <p>Local runtime storage, counts, and latest ingestion errors.</p>
          {lastUpdatedAt ? <span>{formatUpdatedAt(lastUpdatedAt)}</span> : null}
          {isRefreshing ? <span>Refreshing...</span> : null}
        </div>
      </div>
      <div className="grid gap-3 md:grid-cols-4">
        <Metric label="Documents" value={diagnostics.database.documentsCount} />
        <Metric
          label="Chunks"
          value={diagnostics.vectorIndex.documentChunksCount}
        />
        <Metric
          label="Embeddings"
          value={diagnostics.vectorIndex.documentEmbeddingsCount}
        />
        <Metric
          label="Failed jobs"
          value={diagnostics.database.failedIngestionJobsCount}
        />
      </div>
      <div className="grid gap-3 md:grid-cols-4">
        <Metric
          label="DB bytes"
          value={diagnostics.storage.databaseSizeBytes}
        />
        <Metric
          label="Files bytes"
          value={diagnostics.storage.filesSizeBytes}
        />
        <Metric
          label="Index bytes"
          value={diagnostics.storage.indexSizeBytes}
        />
        <Metric label="Logs bytes" value={diagnostics.storage.logsSizeBytes} />
      </div>
      {diagnostics.latestErrors.length > 0 ? (
        <div className="space-y-2">
          <h3 className="text-sm font-medium">Latest errors</h3>
          {diagnostics.latestErrors.map((err) => (
            <div
              key={err.jobId}
              className="rounded-md border border-border bg-muted p-3 text-sm"
            >
              <p className="font-medium">{err.documentName}</p>
              <p className="mt-1 text-muted-foreground">
                {err.errorCode}: {err.errorMessage}
              </p>
            </div>
          ))}
        </div>
      ) : null}
    </section>
  );
}

function Metric({ label, value }: { label: string; value: number }) {
  return (
    <div className="rounded-md border border-border bg-muted p-3">
      <p className="text-xs text-muted-foreground">{label}</p>
      <p className="mt-1 text-lg font-semibold">{value}</p>
    </div>
  );
}

function DiagnosticsPanelSkeleton() {
  return (
    <section
      id="diagnostics"
      className="scroll-mt-6 space-y-5 rounded-xl border border-border bg-card p-5 shadow-sm sm:p-6"
    >
      <div className="space-y-2">
        <Skeleton className="h-5 w-32" />
        <Skeleton className="h-4 w-full max-w-md" />
      </div>
      <div className="grid gap-3 md:grid-cols-4">
        {["Documents", "Chunks", "Embeddings", "Failed jobs"].map((label) => (
          <MetricSkeleton key={label} label={label} />
        ))}
      </div>
      <div className="grid gap-3 md:grid-cols-4">
        {["DB bytes", "Files bytes", "Index bytes", "Logs bytes"].map(
          (label) => (
            <MetricSkeleton key={label} label={label} />
          ),
        )}
      </div>
      <div className="space-y-2">
        <Skeleton className="h-4 w-24" />
        <div className="rounded-md border border-border bg-muted p-3">
          <Skeleton className="h-4 w-48" />
          <Skeleton className="mt-2 h-4 w-full max-w-lg" />
        </div>
      </div>
    </section>
  );
}

function MetricSkeleton({ label }: { label: string }) {
  return (
    <div className="rounded-md border border-border bg-muted p-3">
      <p className="text-xs text-muted-foreground">{label}</p>
      <Skeleton className="mt-2 h-6 w-20" />
    </div>
  );
}

function formatUpdatedAt(timestamp: number) {
  return `Updated ${new Date(timestamp).toLocaleTimeString([], {
    hour: "2-digit",
    minute: "2-digit",
  })}`;
}
