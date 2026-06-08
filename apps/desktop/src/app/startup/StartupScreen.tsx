import { AlertTriangle, Clipboard, FolderOpen, RefreshCw } from "lucide-react";
import { Button, StatusBadge } from "@shared/ui";
import type { AppRuntimeInfo } from "./runtime";

type StartupScreenProps = {
  runtimeInfo: AppRuntimeInfo | null;
  onRetry: () => void;
  onOpenLogs: () => void;
  onCopyDiagnostics: () => void;
  onContinue: () => void;
  canContinue: boolean;
};

export function StartupScreen({
  runtimeInfo,
  onRetry,
  onOpenLogs,
  onCopyDiagnostics,
  onContinue,
  canContinue,
}: StartupScreenProps) {
  const status = runtimeInfo?.localApiStatus ?? "Starting";
  const isProblem = status === "Failed" || status === "Crashed";

  const steps = [
    ["Local API", status],
    ["Database", status === "Ready" ? "Ready" : "Waiting"],
    ["AI runtime", status === "Ready" ? "Checking" : "Waiting"],
    ["Embedding model", status === "Ready" ? "Checking" : "Waiting"],
  ] as const;

  return (
    <main className="flex min-h-screen items-center justify-center bg-background p-6 text-foreground">
      <section className="w-full max-w-2xl space-y-6">
        <div className="space-y-2">
          <StatusBadge
            label={status}
            className={
              isProblem
                ? "border-destructive text-destructive"
                : "border-border text-muted-foreground"
            }
          />
          <h1 className="text-2xl font-semibold">LocalMind is starting</h1>
          <p className="max-w-xl text-sm text-muted-foreground">
            Waiting for the local API before starting diagnostics, runtime, and
            sync requests.
          </p>
        </div>

        <div className="divide-y divide-border rounded-md border border-border bg-card">
          {steps.map(([label, value]) => (
            <div
              className="flex items-center justify-between gap-4 px-4 py-3"
              key={label}
            >
              <span className="text-sm font-medium">{label}</span>
              <span className="text-sm text-muted-foreground">{value}</span>
            </div>
          ))}
        </div>

        {runtimeInfo?.lastError ? (
          <div className="border-destructive text-destructive flex gap-3 rounded-md border bg-background p-4 text-sm">
            <AlertTriangle className="mt-0.5 size-4 shrink-0" />
            <p>{runtimeInfo.lastError}</p>
          </div>
        ) : null}

        <div className="flex flex-wrap gap-2">
          <Button onClick={onRetry}>
            <RefreshCw className="size-4" />
            Retry
          </Button>
          <Button onClick={onOpenLogs} variant="secondary">
            <FolderOpen className="size-4" />
            Open logs
          </Button>
          <Button onClick={onCopyDiagnostics} variant="secondary">
            <Clipboard className="size-4" />
            Copy diagnostics
          </Button>
          {canContinue ? (
            <Button onClick={onContinue} variant="ghost">
              Continue in limited mode
            </Button>
          ) : null}
        </div>
      </section>
    </main>
  );
}
