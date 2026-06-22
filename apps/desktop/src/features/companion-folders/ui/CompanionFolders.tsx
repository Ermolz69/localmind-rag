import { useState } from "react";
import { RefreshCw } from "lucide-react";

import { Button, ConfirmDialog, Toast } from "@shared/ui";
import { useToast } from "@shared/lib/hooks";
import { cn } from "@shared/lib/cn";

import { useCompanionFolders } from "../model/useCompanionFolders";

function healthClass(status: string): string {
  if (status === "Active") {
    return "bg-green-500/10 text-green-500";
  }
  if (status === "Missing" || status === "WatcherError") {
    return "bg-destructive/10 text-destructive";
  }
  return "bg-muted text-muted-foreground";
}

export function CompanionFolders() {
  const {
    status,
    isLoading,
    error,
    rescanningPath,
    isRescanningAll,
    isCleaning,
    rescan,
    cleanup,
  } = useCompanionFolders();
  const { toast, showToast, dismissToast } = useToast();
  const [confirmCleanup, setConfirmCleanup] = useState(false);

  async function handleRescan(path?: string) {
    const result = await rescan(path);
    showToast(result.message, result.success ? "success" : "error");
  }

  async function handleCleanup() {
    const result = await cleanup();
    setConfirmCleanup(false);
    showToast(result.message, result.success ? "success" : "error");
  }

  if (isLoading) {
    return <p className="text-sm text-muted-foreground">Loading folders…</p>;
  }

  if (error) {
    return <p className="text-destructive text-sm">{error}</p>;
  }

  const folders = status?.folders ?? [];

  if (folders.length === 0) {
    return (
      <p className="text-sm text-muted-foreground">
        No watched folders are configured. Add and allow folders on the computer
        first.
      </p>
    );
  }

  return (
    <div className="flex min-h-0 flex-1 flex-col gap-3">
      <ul className="flex min-h-0 flex-1 flex-col gap-2 overflow-y-auto">
        {folders.map((folder) => (
          <li
            key={folder.path}
            className="rounded-xl border border-border bg-card p-3"
          >
            <p
              className="truncate text-sm font-medium text-foreground"
              title={folder.path}
            >
              {folder.path}
            </p>
            <div className="mt-1 flex flex-wrap items-center gap-2 text-xs">
              <span
                className={cn(
                  "rounded-full px-2 py-0.5 font-medium",
                  healthClass(folder.healthStatus),
                )}
              >
                {folder.healthStatus}
              </span>
              <span className="text-muted-foreground">
                {folder.activeDocumentsCount} document(s)
              </span>
              {Number(folder.deletedWaitingCleanupCount) > 0 ? (
                <span className="text-destructive font-medium">
                  {folder.deletedWaitingCleanupCount} pending cleanup
                </span>
              ) : null}
            </div>
            {folder.lastError ? (
              <p className="text-destructive mt-1 text-xs">
                Error: {folder.lastError}
              </p>
            ) : null}
            <div className="mt-2 flex justify-end">
              <Button
                variant="secondary"
                disabled={!folder.enabled || rescanningPath === folder.path}
                onClick={() => void handleRescan(folder.path)}
              >
                <RefreshCw className="h-4 w-4" />
                {rescanningPath === folder.path ? "Rescanning…" : "Rescan"}
              </Button>
            </div>
          </li>
        ))}
      </ul>

      <div className="flex flex-wrap gap-2">
        <Button
          variant="secondary"
          disabled={isRescanningAll}
          onClick={() => void handleRescan()}
        >
          {isRescanningAll ? "Rescanning…" : "Rescan all"}
        </Button>
        <Button
          variant="secondary"
          disabled={isCleaning}
          onClick={() => setConfirmCleanup(true)}
        >
          Cleanup deleted files
        </Button>
      </div>

      <ConfirmDialog
        open={confirmCleanup}
        title="Cleanup deleted files?"
        description="Removes internal records for files deleted from your watched folders. Your original files are not affected."
        confirmLabel="Cleanup"
        isPending={isCleaning}
        onConfirm={() => void handleCleanup()}
        onClose={() => setConfirmCleanup(false)}
      />

      <Toast
        message={toast?.message ?? null}
        variant={toast?.variant}
        onDismiss={dismissToast}
      />
    </div>
  );
}
