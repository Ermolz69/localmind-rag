/** Tailwind background class for an activity event's status dot, keyed by kind. */
export function activityDotClass(kind: string): string {
  if (kind === "ingestion.indexed") {
    return "bg-green-500";
  }
  if (kind === "ingestion.failed") {
    return "bg-destructive";
  }
  if (kind === "document.added" || kind === "watched.found") {
    return "bg-primary";
  }
  return "bg-muted-foreground";
}

/** Short local time (e.g. "12:30") for an activity event timestamp. */
export function formatActivityTime(timestamp: string): string {
  return new Date(timestamp).toLocaleTimeString([], {
    hour: "2-digit",
    minute: "2-digit",
  });
}
