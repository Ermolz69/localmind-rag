import { useCompanionDocuments } from "@features/companion-documents";
import { resolveDocumentPhase, documentPhaseClass } from "@entities/document";
import { cn } from "@shared/lib/cn";

export type RecentlyAddedItem = { id: string; name: string };

/**
 * Shows files just added from the phone with their live processing status, so the
 * "Add to LocalMind" loop closes on the same screen: accepted → waiting →
 * processing → ready / failed. Reuses the documents hook, which polls while work
 * is in flight.
 */
export function RecentlyAddedFiles({ items }: { items: RecentlyAddedItem[] }) {
  const { documents } = useCompanionDocuments();
  const byId = new Map(documents.map((document) => [document.id, document]));

  return (
    <div className="rounded-xl border border-border bg-muted/30 p-3">
      <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
        Recently added
      </p>
      <ul className="mt-2 flex flex-col gap-2">
        {items.map((item) => {
          const document = byId.get(item.id);
          // Until the backend reports a status, the file is simply "accepted".
          const { phase, label } = document
            ? resolveDocumentPhase(document.status)
            : { phase: "accepted" as const, label: "Accepted" };

          return (
            <li key={item.id}>
              <div className="flex items-center justify-between gap-3">
                <span
                  className="min-w-0 truncate text-sm text-foreground"
                  title={item.name}
                >
                  {item.name}
                </span>
                <span
                  className={cn(
                    "shrink-0 text-xs font-medium",
                    documentPhaseClass(phase),
                  )}
                >
                  {label}
                </span>
              </div>
              {phase === "failed" && document?.errorMessage ? (
                <p className="mt-0.5 text-xs text-muted-foreground">
                  {document.errorMessage}
                </p>
              ) : null}
            </li>
          );
        })}
      </ul>
    </div>
  );
}
