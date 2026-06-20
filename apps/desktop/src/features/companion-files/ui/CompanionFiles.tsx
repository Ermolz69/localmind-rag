import { ChevronUp, File, Folder, Home, Plus } from "lucide-react";

import { Button, Toast } from "@shared/ui";
import { useToast } from "@shared/lib/hooks";

import { useCompanionFiles } from "../model/useCompanionFiles";

export function CompanionFiles() {
  const {
    roots,
    current,
    isLoading,
    error,
    addingPath,
    browse,
    goToRoots,
    addFile,
  } = useCompanionFiles();
  const { toast, showToast, dismissToast } = useToast();

  async function handleAdd(path: string) {
    const result = await addFile(path);
    showToast(result.message, result.success ? "success" : "error");
  }

  const parentPath = current?.parentPath ?? null;

  return (
    <div className="flex min-h-0 flex-1 flex-col gap-3">
      {current ? (
        <div className="flex items-center gap-2">
          <Button
            variant="secondary"
            onClick={goToRoots}
            aria-label="All folders"
          >
            <Home className="h-4 w-4" />
          </Button>
          {parentPath ? (
            <Button variant="secondary" onClick={() => void browse(parentPath)}>
              <ChevronUp className="h-4 w-4" />
              Up
            </Button>
          ) : null}
          <span
            className="min-w-0 flex-1 truncate text-xs text-muted-foreground"
            title={current.path}
          >
            {current.path}
          </span>
        </div>
      ) : null}

      {error ? <p className="text-destructive text-sm">{error}</p> : null}

      <div className="flex min-h-0 flex-1 flex-col gap-2 overflow-y-auto">
        {isLoading ? (
          <p className="text-sm text-muted-foreground">Loading…</p>
        ) : current === null ? (
          roots.length === 0 ? (
            <p className="text-sm text-muted-foreground">
              No folders are shared yet. Allow folders on the computer first.
            </p>
          ) : (
            roots.map((root) => (
              <button
                key={root.path}
                type="button"
                onClick={() => void browse(root.path)}
                className="flex items-center gap-3 rounded-xl border border-border bg-card p-3 text-left"
              >
                <Folder className="h-5 w-5 shrink-0 text-primary" />
                <span
                  className="truncate text-sm font-medium text-foreground"
                  title={root.path}
                >
                  {root.name}
                </span>
              </button>
            ))
          )
        ) : current.entries.length === 0 ? (
          <p className="text-sm text-muted-foreground">
            This folder has no subfolders or supported files.
          </p>
        ) : (
          current.entries.map((entry) =>
            entry.isDirectory ? (
              <button
                key={entry.path}
                type="button"
                onClick={() => void browse(entry.path)}
                className="flex items-center gap-3 rounded-xl border border-border bg-card p-3 text-left"
              >
                <Folder className="h-5 w-5 shrink-0 text-primary" />
                <span className="truncate text-sm font-medium text-foreground">
                  {entry.name}
                </span>
              </button>
            ) : (
              <div
                key={entry.path}
                className="flex items-center justify-between gap-3 rounded-xl border border-border bg-card p-3"
              >
                <div className="flex min-w-0 items-center gap-3">
                  <File className="h-5 w-5 shrink-0 text-muted-foreground" />
                  <span className="truncate text-sm text-foreground">
                    {entry.name}
                  </span>
                </div>
                <Button
                  variant="secondary"
                  className="shrink-0"
                  disabled={addingPath === entry.path}
                  onClick={() => void handleAdd(entry.path)}
                >
                  <Plus className="h-4 w-4" />
                  {addingPath === entry.path ? "Adding…" : "Add"}
                </Button>
              </div>
            ),
          )
        )}
      </div>

      <Toast
        message={toast?.message ?? null}
        variant={toast?.variant}
        onDismiss={dismissToast}
      />
    </div>
  );
}
