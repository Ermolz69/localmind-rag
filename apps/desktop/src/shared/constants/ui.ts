export const documentStatusStyles: Record<string, string> = {
  Pending: "border-border bg-muted text-muted-foreground",
  Queued: "border-border bg-muted text-muted-foreground",
  Processing: "border-primary bg-primary text-primary-foreground",
  Chunking: "border-primary bg-primary text-primary-foreground",
  Embedding: "border-primary bg-primary text-primary-foreground",
  Indexed: "border-accent bg-accent text-accent-foreground",
  Failed: "border-destructive bg-destructive text-destructive-foreground",
  Cancelled: "border-border bg-card text-muted-foreground",
};

export const runtimeStateStyles: Record<string, string> = {
  ready: "border-accent bg-accent text-accent-foreground",
  warning: "border-border bg-muted text-muted-foreground",
  offline: "border-border bg-card text-muted-foreground",
};
export const ACTIVE_INGESTION_JOB_STATUSES = new Set([
  "Pending",
  "Processing",
  "Chunking",
  "Embedding",
]);
