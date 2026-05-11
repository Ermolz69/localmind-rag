import type { RagSource } from "../../shared/api/client";

export function SourcePanel({ sources }: { sources: RagSource[] }) {
  return (
    <section className="space-y-2">
      {sources.map((source) => (
        <article
          key={source.chunkId}
          className="rounded-md border border-border bg-card p-3"
        >
          <div className="text-sm font-medium">{source.documentName}</div>
          <p className="mt-1 text-sm text-muted-foreground">{source.snippet}</p>
        </article>
      ))}
    </section>
  );
}
