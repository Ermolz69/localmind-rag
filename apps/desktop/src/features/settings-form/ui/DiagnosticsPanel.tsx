import type { DiagnosticsStatus } from "@entities/runtime";

type DiagnosticsPanelProps = {
  diagnostics: DiagnosticsStatus | null;
};

export function DiagnosticsPanel({ diagnostics }: DiagnosticsPanelProps) {
  if (!diagnostics) {
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
        <p className="text-sm text-muted-foreground">
          Local runtime storage, counts, and latest ingestion errors.
        </p>
      </div>
      <div className="grid gap-3 md:grid-cols-4">
        <Metric label="Documents" value={diagnostics.counts.documentsCount} />
        <Metric label="Chunks" value={diagnostics.counts.documentChunksCount} />
        <Metric
          label="Embeddings"
          value={diagnostics.counts.documentEmbeddingsCount}
        />
        <Metric
          label="Failed jobs"
          value={diagnostics.counts.failedIngestionJobsCount}
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
          {diagnostics.latestErrors.map((error) => (
            <div
              key={error.jobId}
              className="rounded-md border border-border bg-muted p-3 text-sm"
            >
              <p className="font-medium">{error.documentName}</p>
              <p className="mt-1 text-muted-foreground">{error.lastError}</p>
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
