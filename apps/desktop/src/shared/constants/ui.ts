export const documentStatusStyles: Record<string, string> = {
  Queued: "border-border bg-muted text-muted-foreground",
  Processing: "border-primary bg-primary text-primary-foreground",
  Indexed: "border-accent bg-accent text-accent-foreground",
  Failed: "border-border bg-card text-foreground",
};

export const runtimeStateStyles: Record<string, string> = {
  ready: "border-accent bg-accent text-accent-foreground",
  warning: "border-border bg-muted text-muted-foreground",
  offline: "border-border bg-card text-muted-foreground",
};
