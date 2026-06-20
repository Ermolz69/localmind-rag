import { cn } from "@shared/lib/cn";

import {
  ACTIVE_DOCUMENT_STATUSES,
  useCompanionDocuments,
  type CompanionDocument,
} from "../model/useCompanionDocuments";

function statusLabel(document: CompanionDocument): string {
  if (
    ACTIVE_DOCUMENT_STATUSES.has(document.status) &&
    document.progressPercent !== null &&
    document.status !== "Pending"
  ) {
    return `${document.status} ${document.progressPercent}%`;
  }

  return document.status;
}

function statusClass(status: string): string {
  if (status === "Indexed") {
    return "text-green-500";
  }
  if (status === "Failed") {
    return "text-destructive";
  }
  if (ACTIVE_DOCUMENT_STATUSES.has(status)) {
    return "text-accent";
  }
  return "text-muted-foreground";
}

function Summary({ documents }: { documents: CompanionDocument[] }) {
  const ready = documents.filter((d) => d.status === "Indexed").length;
  const processing = documents.filter(
    (d) => ACTIVE_DOCUMENT_STATUSES.has(d.status) && d.status !== "Pending",
  ).length;
  const waiting = documents.filter((d) => d.status === "Pending").length;
  const failed = documents.filter((d) => d.status === "Failed").length;

  return (
    <p className="text-xs text-muted-foreground">
      {ready} ready · {processing} processing · {waiting} waiting · {failed}{" "}
      failed
    </p>
  );
}

export function CompanionDocuments() {
  const { documents, isLoading, error } = useCompanionDocuments();

  if (isLoading) {
    return <p className="text-sm text-muted-foreground">Loading documents…</p>;
  }

  if (error) {
    return <p className="text-destructive text-sm">{error}</p>;
  }

  if (documents.length === 0) {
    return (
      <p className="text-sm text-muted-foreground">
        No documents yet. Add documents on the computer to search them here.
      </p>
    );
  }

  return (
    <div className="flex min-h-0 flex-1 flex-col gap-3">
      <Summary documents={documents} />

      <ul className="flex min-h-0 flex-1 flex-col gap-2 overflow-y-auto">
        {documents.map((document) => (
          <li
            key={document.id}
            className="rounded-xl border border-border bg-card p-3"
          >
            <p className="truncate text-sm font-medium text-foreground">
              {document.name}
            </p>
            <p className="mt-0.5 text-xs">
              <span className="text-muted-foreground">Status: </span>
              <span className={cn("font-medium", statusClass(document.status))}>
                {statusLabel(document)}
              </span>
            </p>
            {document.status === "Failed" && document.errorMessage ? (
              <p className="mt-0.5 text-xs text-muted-foreground">
                Reason: {document.errorMessage}
              </p>
            ) : null}
          </li>
        ))}
      </ul>
    </div>
  );
}
