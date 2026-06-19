import { FileText, PanelLeft } from "lucide-react";
import {
  EditorTabBar,
  EditorToolbar,
  NoteEditor,
  NotePropertiesPanel,
} from "@features/note-editor";
import { VaultExplorer } from "@features/vault-explorer";
import { ConfirmDialog, ErrorBanner } from "@shared/ui";
import { useNotesPageViewModel } from "./model/useNotesPageViewModel";
import { ExplorerResizeHandle } from "./ui/ExplorerResizeHandle";
import { useLocalStorage } from "@shared/lib/hooks";

const MIN_EXPLORER_WIDTH = 240;
const COLLAPSE_THRESHOLD = 160;
const MAX_EXPLORER_WIDTH = 520;
const DEFAULT_EXPLORER_WIDTH = 320;

export function NotesPage() {
  const page = useNotesPageViewModel();

  const [explorerWidthStr, setExplorerWidthStr] = useLocalStorage(
    "notesExplorerWidth",
    String(DEFAULT_EXPLORER_WIDTH),
  );
  const explorerWidth =
    parseInt(explorerWidthStr, 10) || DEFAULT_EXPLORER_WIDTH;

  const [isExplorerCollapsedStr, setIsExplorerCollapsedStr] = useLocalStorage(
    "notesExplorerCollapsed",
    "false",
  );
  const isExplorerCollapsed = isExplorerCollapsedStr === "true";

  const handleMouseDown = (e: React.MouseEvent) => {
    e.preventDefault();
    const startX = e.clientX;
    const startWidth = explorerWidth;

    let currentCollapsed = isExplorerCollapsed;

    document.body.style.cursor = "col-resize";
    document.body.style.userSelect = "none";

    const handleMouseMove = (moveEvent: MouseEvent) => {
      const newWidth = startWidth + (moveEvent.clientX - startX);

      if (newWidth < COLLAPSE_THRESHOLD) {
        if (!currentCollapsed) {
          setIsExplorerCollapsedStr("true");
          currentCollapsed = true;
        }
      } else {
        if (currentCollapsed) {
          setIsExplorerCollapsedStr("false");
          currentCollapsed = false;
        }
        if (newWidth >= MIN_EXPLORER_WIDTH) {
          const clampedWidth = Math.min(newWidth, MAX_EXPLORER_WIDTH);
          setExplorerWidthStr(String(clampedWidth));
        }
      }
    };

    const handleMouseUp = () => {
      document.removeEventListener("mousemove", handleMouseMove);
      document.removeEventListener("mouseup", handleMouseUp);
      document.body.style.cursor = "";
      document.body.style.userSelect = "";
    };

    document.addEventListener("mousemove", handleMouseMove);
    document.addEventListener("mouseup", handleMouseUp);
  };

  return (
    <section className="flex h-[calc(100dvh-5.5rem)] min-h-0 flex-col overflow-hidden">
      <ErrorBanner message={page.error} />

      <div
        className="relative grid min-h-0 flex-1 overflow-hidden"
        style={{
          gridTemplateColumns: isExplorerCollapsed
            ? "40px minmax(0, 1fr)"
            : `${explorerWidth}px 6px minmax(0, 1fr)`,
        }}
      >
        {isExplorerCollapsed ? (
          <div className="flex flex-col items-center border-r border-border bg-card pt-2">
            <button
              className="rounded-md p-2 text-muted-foreground transition-colors hover:bg-muted hover:text-foreground"
              onClick={() => setIsExplorerCollapsedStr("false")}
              title="Show Explorer"
            >
              <PanelLeft size={20} />
            </button>
          </div>
        ) : (
          <>
            <div className="min-h-0 min-w-0 overflow-hidden">
              <VaultExplorer
                explorer={page.explorer}
                onOpenNote={(noteId) => {
                  const note = page.explorer.notes.find((n) => n.id === noteId);
                  if (note) void page.tabs.openTab(note);
                }}
                onOpenNoteInNewTab={(noteId) => {
                  const note = page.explorer.notes.find((n) => n.id === noteId);
                  if (note) page.tabs.openInNewTab(note);
                }}
                onShowProperties={(noteId) => {
                  const note = page.explorer.notes.find((n) => n.id === noteId);
                  if (note) {
                    void page.tabs.openTab(note);
                    page.setIsPropertiesOpen(true);
                  }
                }}
              />
            </div>

            <ExplorerResizeHandle
              onMouseDown={handleMouseDown}
              onDoubleClick={() => setIsExplorerCollapsedStr("true")}
            />
          </>
        )}

        <div className="relative flex min-w-0 flex-col overflow-hidden">
          <EditorTabBar
            tabs={page.tabs.openTabs}
            activeTabId={page.tabs.activeTabId}
            onTabClick={(id) => page.tabs.setActiveTabId(id)}
            onTabClose={(id) => void page.tabs.closeTab(id)}
            onMiddleClick={(id) => void page.tabs.closeTab(id)}
          />

          <EditorToolbar
            viewMode={page.editorViewMode}
            onViewModeChange={page.setEditorViewMode}
            isDirty={page.isDirty}
            onSave={() => void page.saveNote()}
          />

          <div className="min-h-0 min-w-0 flex-1">
            {page.tabs.activeTabId ? (
              <NoteEditor
                draft={page.activeDraft}
                viewMode={page.editorViewMode}
                onDraftChange={page.setDraft}
              />
            ) : (
              <div className="flex h-full items-center justify-center rounded-md border border-border bg-card">
                <div className="flex flex-col items-center gap-2 text-muted-foreground">
                  <FileText size={32} />
                  <p>Select a markdown file to edit</p>
                </div>
              </div>
            )}
          </div>

          {page.isPropertiesOpen && page.explorer.selectedNoteId && (
            <NotePropertiesPanel
              note={
                page.explorer.notes.find(
                  (n) => n.id === page.explorer.selectedNoteId,
                )!
              }
              buckets={page.explorer.buckets}
              isOpen={page.isPropertiesOpen}
              onClose={() => page.setIsPropertiesOpen(false)}
              onDelete={() => {
                page.setDeleteTargetId(page.explorer.selectedNoteId);
                page.setIsPropertiesOpen(false);
              }}
            />
          )}
        </div>
      </div>

      <ConfirmDialog
        open={Boolean(page.deleteTargetId)}
        title="Delete file"
        description="This removes the markdown file from your local vault."
        confirmLabel="Delete"
        isPending={page.explorer.isDeletingNote}
        onConfirm={() => void page.deleteNote()}
        onClose={() => page.setDeleteTargetId(null)}
      />
    </section>
  );
}
