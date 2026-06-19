import { BookOpen, Code2, Eye, Save } from "lucide-react";
import { cn } from "@shared/lib/cn";
import type { EditorViewMode } from "../model/types";

type EditorToolbarProps = {
  viewMode: EditorViewMode;
  onViewModeChange: (mode: EditorViewMode) => void;
  isDirty: boolean;
  onSave: () => void;
};

export function EditorToolbar({
  viewMode,
  onViewModeChange,
  isDirty,
  onSave,
}: EditorToolbarProps) {
  return (
    <div className="flex h-9 items-center justify-between border-b border-border bg-card px-2">
      <div className="flex items-center gap-1">
        <button
          type="button"
          title="Source"
          onClick={() => onViewModeChange("source")}
          className={cn(
            "inline-flex h-7 items-center gap-1.5 rounded px-2 text-xs text-muted-foreground transition-colors hover:bg-muted hover:text-foreground",
            viewMode === "source" && "bg-muted text-foreground",
          )}
        >
          <Code2 size={14} />
          <span>Source</span>
        </button>
        <button
          type="button"
          title="Live Preview"
          onClick={() => onViewModeChange("live-preview")}
          className={cn(
            "inline-flex h-7 items-center gap-1.5 rounded px-2 text-xs text-muted-foreground transition-colors hover:bg-muted hover:text-foreground",
            viewMode === "live-preview" && "bg-muted text-foreground",
          )}
        >
          <Eye size={14} />
          <span>Live Preview</span>
        </button>
        <button
          type="button"
          title="Reading"
          onClick={() => onViewModeChange("reading")}
          className={cn(
            "inline-flex h-7 items-center gap-1.5 rounded px-2 text-xs text-muted-foreground transition-colors hover:bg-muted hover:text-foreground",
            viewMode === "reading" && "bg-muted text-foreground",
          )}
        >
          <BookOpen size={14} />
          <span>Reading</span>
        </button>
      </div>

      <div className="flex items-center">
        <button
          type="button"
          title="Save (Ctrl+S)"
          onClick={onSave}
          disabled={!isDirty}
          className={cn(
            "flex h-7 items-center gap-1.5 rounded px-2 text-xs font-medium transition-colors",
            isDirty
              ? "text-primary hover:bg-muted"
              : "cursor-not-allowed text-muted-foreground opacity-50",
          )}
        >
          <Save size={13} />
          Save
        </button>
      </div>
    </div>
  );
}
