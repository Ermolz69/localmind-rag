/**
 * The five user-facing stages a document moves through, independent of the many
 * internal document/ingestion status names. Used to give the phone (and anywhere
 * else) one clear vocabulary: accepted → waiting → processing → ready / failed.
 */
export type DocumentPhase =
  | "accepted"
  | "waiting"
  | "processing"
  | "ready"
  | "failed";

export type DocumentPhaseInfo = { phase: DocumentPhase; label: string };

/**
 * Maps a raw document/ingestion status (e.g. "Queued", "Chunking", "Indexed") to
 * a friendly lifecycle phase. Unknown values fall back to "accepted" — the state a
 * just-added file is in before the backend reports anything.
 */
export function resolveDocumentPhase(status: string): DocumentPhaseInfo {
  switch (status) {
    case "Indexed":
      return { phase: "ready", label: "Ready to search" };
    case "Failed":
    case "Cancelled":
      return { phase: "failed", label: "Couldn't process" };
    case "Processing":
    case "Chunking":
    case "Embedding":
      return { phase: "processing", label: "Processing" };
    case "Pending":
    case "Queued":
    case "Uploaded":
    case "Draft":
      return { phase: "waiting", label: "Waiting to process" };
    default:
      return { phase: "accepted", label: "Accepted" };
  }
}

/** Tailwind text-color class for a phase, so badges read consistently. */
export function documentPhaseClass(phase: DocumentPhase): string {
  switch (phase) {
    case "ready":
      return "text-green-500";
    case "failed":
      return "text-destructive";
    case "processing":
    case "waiting":
      return "text-accent";
    default:
      return "text-muted-foreground";
  }
}
