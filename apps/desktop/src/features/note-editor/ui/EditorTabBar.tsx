import { FileText, X } from "lucide-react";
import { cn } from "@shared/lib/cn";
import type { OpenNoteTab } from "../model/types";

type EditorTabBarProps = {
  tabs: OpenNoteTab[];
  activeTabId: string | null;
  onTabClick: (noteId: string) => void;
  onTabClose: (noteId: string) => void;
  onMiddleClick: (noteId: string) => void;
};

export function EditorTabBar({
  tabs,
  activeTabId,
  onTabClick,
  onTabClose,
  onMiddleClick,
}: EditorTabBarProps) {
  if (tabs.length === 0) {
    return null;
  }

  return (
    <div className="scrollbar-thin scrollbar-track-transparent scrollbar-thumb-neutral-200 dark:scrollbar-thumb-neutral-800 flex h-9 w-full overflow-x-auto overflow-y-hidden border-b border-border bg-muted/30">
      <div className="flex h-full min-w-max">
        {tabs.map((tab) => {
          const isActive = tab.noteId === activeTabId;
          return (
            <div
              key={tab.noteId}
              className={cn(
                "group flex h-full max-w-[200px] cursor-pointer items-center gap-2 border-r border-border px-3 text-xs transition-colors",
                isActive
                  ? "bg-background text-foreground shadow-[inset_0_-2px_0_0_hsl(var(--primary))]"
                  : "bg-transparent text-muted-foreground hover:bg-muted/50 hover:text-foreground",
              )}
              onClick={() => onTabClick(tab.noteId)}
              onAuxClick={(e) => {
                if (e.button === 1) {
                  e.preventDefault();
                  onMiddleClick(tab.noteId);
                }
              }}
            >
              <FileText size={14} className="shrink-0 opacity-70" aria-hidden />

              <span className="select-none truncate">
                {tab.title || "Untitled"}
                <span className="opacity-50">.md</span>
              </span>

              <div className="flex w-4 shrink-0 items-center justify-center">
                {tab.isDirty ? (
                  <span
                    className={cn(
                      "h-2 w-2 rounded-full",
                      isActive ? "bg-foreground" : "bg-muted-foreground",
                      "group-hover:hidden",
                    )}
                  />
                ) : null}
                <button
                  type="button"
                  className={cn(
                    "flex h-4 w-4 items-center justify-center rounded-sm hover:bg-muted",
                    tab.isDirty
                      ? "hidden group-hover:flex"
                      : "opacity-0 group-hover:opacity-100",
                    isActive && !tab.isDirty ? "opacity-100" : "",
                  )}
                  onClick={(e) => {
                    e.stopPropagation();
                    onTabClose(tab.noteId);
                  }}
                  title="Close tab"
                >
                  <X size={12} />
                </button>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}
