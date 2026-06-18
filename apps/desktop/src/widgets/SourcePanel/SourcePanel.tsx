import { BarChart3, FileText, Fingerprint, Layers3 } from "lucide-react";
import { useState } from "react";
import type { RagSource } from "@entities/source";
import { cn } from "@shared/lib/cn";

type SourceTab = "sources" | "snippets" | "metadata";

const tabs: Array<{ id: SourceTab; label: string }> = [
  { id: "sources", label: "Sources" },
  { id: "snippets", label: "Snippets" },
  { id: "metadata", label: "Metadata" },
];

function formatScore(score: number | string) {
  const value = Number(score);
  if (Number.isNaN(value)) {
    return "n/a";
  }

  return value.toFixed(2);
}

function scoreTone(score: number | string) {
  const value = Number(score);
  if (Number.isNaN(value)) {
    return "border-border bg-muted text-muted-foreground";
  }

  if (value >= 0.75) {
    return "border-primary/40 bg-primary/10 text-primary";
  }

  return "border-border bg-muted text-muted-foreground";
}

export function SourcePanel({ sources }: { sources: RagSource[] }) {
  const [activeTab, setActiveTab] = useState<SourceTab>("sources");

  if (sources.length === 0) {
    return (
      <section className="space-y-3">
        <div className="rounded-xl border border-border bg-gradient-to-br from-muted/40 via-card to-background p-4 shadow-inner">
          <div className="flex h-11 w-11 items-center justify-center rounded-xl border border-primary/30 bg-primary/10 text-primary">
            <Layers3 size={20} aria-hidden />
          </div>
          <h3 className="mt-4 text-sm font-semibold">Evidence system idle</h3>
          <p className="mt-2 text-sm leading-6 text-muted-foreground">
            Selected-answer sources, confidence signals, and extracted snippets
            appear here after a grounded response.
          </p>
        </div>

        <div className="grid gap-2">
          {["Document match", "Relevant chunk", "Answer metadata"].map(
            (label) => (
              <div
                key={label}
                className="rounded-lg border border-dashed border-border bg-background/60 p-3"
              >
                <div className="flex items-center gap-2">
                  <span className="flex h-8 w-8 items-center justify-center rounded-md border border-border bg-card text-muted-foreground">
                    <FileText size={14} aria-hidden />
                  </span>
                  <div className="min-w-0 flex-1">
                    <div className="h-2 w-2/3 rounded-full bg-muted" />
                    <div className="mt-2 h-2 w-1/2 rounded-full bg-muted/70" />
                  </div>
                  <span className="rounded-full border border-border bg-muted px-2 py-0.5 text-[11px] text-muted-foreground">
                    {label}
                  </span>
                </div>
              </div>
            ),
          )}
        </div>
      </section>
    );
  }

  return (
    <section className="space-y-3">
      <div className="grid grid-cols-3 rounded-lg border border-border bg-background/70 p-1 shadow-inner">
        {tabs.map((tab) => (
          <button
            key={tab.id}
            type="button"
            className={cn(
              "rounded-md px-2 py-1.5 text-xs font-medium transition",
              activeTab === tab.id
                ? "bg-primary text-primary-foreground shadow-sm"
                : "text-muted-foreground hover:bg-muted hover:text-foreground",
            )}
            onClick={() => setActiveTab(tab.id)}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {activeTab === "sources" ? (
        <div className="space-y-2.5">
          {sources.map((source, index) => (
            <article
              key={source.chunkId}
              className="rounded-xl border border-border bg-background/80 p-3 shadow-sm transition hover:border-primary/40"
            >
              <div className="flex items-start justify-between gap-3">
                <div className="flex min-w-0 gap-2">
                  <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg border border-border bg-card text-primary">
                    <FileText size={15} aria-hidden />
                  </span>
                  <div className="min-w-0">
                    <div className="truncate text-sm font-semibold">
                      {source.documentName}
                    </div>
                    <div className="mt-1 text-xs text-muted-foreground">
                      {source.pageNumber
                        ? `Page ${source.pageNumber}`
                        : "Page not available"}
                    </div>
                  </div>
                </div>
                <div
                  className={cn(
                    "shrink-0 rounded-full border px-2 py-0.5 text-[11px] font-medium",
                    scoreTone(source.score),
                  )}
                >
                  {formatScore(source.score)}
                </div>
              </div>
              <p className="mt-3 line-clamp-5 whitespace-pre-wrap text-sm leading-6 text-muted-foreground">
                {source.snippet}
              </p>
              <div className="mt-3 flex items-center justify-between text-[11px] text-muted-foreground">
                <span>Evidence #{index + 1}</span>
                <span>{source.chunkId.slice(0, 8)}</span>
              </div>
            </article>
          ))}
        </div>
      ) : null}

      {activeTab === "snippets" ? (
        <div className="space-y-2.5">
          {sources.map((source) => (
            <article
              key={source.chunkId}
              className="rounded-xl border border-border bg-background/80 p-3"
            >
              <div className="mb-2 flex items-center gap-2 text-xs font-semibold text-muted-foreground">
                <BarChart3 size={14} aria-hidden />
                {source.documentName}
              </div>
              <p className="whitespace-pre-wrap text-sm leading-6 text-foreground">
                {source.snippet}
              </p>
            </article>
          ))}
        </div>
      ) : null}

      {activeTab === "metadata" ? (
        <div className="space-y-2.5">
          {sources.map((source) => (
            <article
              key={source.chunkId}
              className="rounded-xl border border-border bg-background/80 p-3"
            >
              <div className="flex items-center gap-2 text-sm font-semibold">
                <Fingerprint size={15} className="text-primary" aria-hidden />
                Retrieval metadata
              </div>
              <dl className="mt-3 grid gap-2 text-xs text-muted-foreground">
                <div className="flex items-center justify-between gap-3">
                  <dt>Document</dt>
                  <dd className="truncate text-foreground">
                    {source.documentName}
                  </dd>
                </div>
                <div className="flex items-center justify-between gap-3">
                  <dt>Page</dt>
                  <dd>{source.pageNumber ?? "n/a"}</dd>
                </div>
                <div className="flex items-center justify-between gap-3">
                  <dt>Score</dt>
                  <dd>{formatScore(source.score)}</dd>
                </div>
                <div className="flex items-center justify-between gap-3">
                  <dt>Chunk</dt>
                  <dd className="font-mono">{source.chunkId.slice(0, 8)}</dd>
                </div>
              </dl>
            </article>
          ))}
        </div>
      ) : null}
    </section>
  );
}
