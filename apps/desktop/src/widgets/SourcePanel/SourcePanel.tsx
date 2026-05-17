import type { RagSource } from "../../shared/api/client";
import { EmptyState } from "../../shared/ui/EmptyState";

export function SourcePanel({ sources }: { sources: RagSource[] }) {
  if (sources.length === 0) {
    return (
      <EmptyState
        title="No source snippets"
        description="Ask a question to surface the local chunks used to build the answer."
      />
    );
  }

  return (
    <section className="space-y-3">
      {sources.map((source) => (
        <article
          key={source.chunkId}
          className="rounded-md border border-border bg-card p-3"
        >
          <div className="flex items-start justify-between gap-3">
            <div>
              <div className="text-sm font-medium">{source.documentName}</div>
              <div className="mt-1 text-xs text-muted-foreground">
                {source.pageNumber
                  ? `Page ${source.pageNumber}`
                  : "Page not available"}
              </div>
            </div>
            <div className="rounded-full bg-muted px-2 py-1 text-[11px] text-muted-foreground">
              Score {source.score.toFixed(2)}
            </div>
          </div>
          <p className="mt-3 whitespace-pre-wrap text-sm leading-6 text-muted-foreground">
            {source.snippet}
          </p>
        </article>
      ))}
    </section>
  );
}
