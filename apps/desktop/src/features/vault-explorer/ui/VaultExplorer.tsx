import { useEffect, useState } from "react";
import {
  ChevronDown,
  ChevronRight,
  FolderPlus,
  Loader2,
  Plus,
  Move,
  Trash2,
} from "lucide-react";
import { BucketSelector } from "@features/note-editor";
import { Button, ConfirmDialog, Modal } from "@shared/ui";
import { cn } from "@shared/lib/cn";
import { getVaultItemDragData } from "../model/dragPayload";
import { useVaultExplorer } from "../model/useVaultExplorer";
import { ExplorerNode } from "./ExplorerNode";

export function VaultExplorer({
  explorer,
  onOpenNote,
  onOpenNoteInNewTab,
  onShowProperties,
}: {
  explorer: ReturnType<typeof useVaultExplorer>;
  onOpenNote?: (noteId: string) => void;
  onOpenNoteInNewTab?: (noteId: string) => void;
  onShowProperties?: (noteId: string) => void;
}) {
  const [isVaultScopeOpen, setIsVaultScopeOpen] = useState(true);
  const [inlineInput, setInlineInput] = useState<
    | { type: "createFile"; parentFolderId: string | null }
    | { type: "createFolder"; parentFolderId: string | null }
    | {
        type: "rename";
        itemType: "note" | "folder";
        id: string;
        initialName: string;
      }
    | null
  >(null);
  const [isRootDropActive, setIsRootDropActive] = useState(false);
  const [contextMenu, setContextMenu] = useState<{
    x: number;
    y: number;
    type: "note" | "folder" | "root";
    id: string | null;
    name: string | null;
  } | null>(null);
  const [moveTarget, setMoveTarget] = useState<{
    type: "note" | "folder";
    id: string;
    name: string;
  } | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<{
    type: "note" | "folder";
    id: string;
    name: string;
  } | null>(null);
  const [targetBucketId, setTargetBucketId] = useState<string>("");

  useEffect(() => {
    const closeMenu = () => setContextMenu(null);
    window.addEventListener("click", closeMenu);
    return () => window.removeEventListener("click", closeMenu);
  }, []);

  const handleRootDrop = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setIsRootDropActive(false);
    const data = getVaultItemDragData(e.dataTransfer);
    if (!data) {
      return;
    }

    if (data.type === "note") {
      void explorer.moveNote(data.id, explorer.selectedBucketId, null);
    } else {
      void explorer.moveFolder(data.id, explorer.selectedBucketId, null);
    }
  };

  const handleInlineSubmit = async (value: string) => {
    if (!inlineInput) return;

    if (inlineInput.type === "createFolder") {
      await explorer.createFolder(value, inlineInput.parentFolderId);
    } else if (inlineInput.type === "createFile") {
      await explorer.createNote(value, inlineInput.parentFolderId);
    } else if (inlineInput.type === "rename") {
      if (inlineInput.itemType === "folder") {
        await explorer.renameFolder(
          inlineInput.id,
          value,
          explorer.folders.find((f) => f.id === inlineInput.id)
            ?.parentFolderId ?? null,
        );
      } else {
        const note = explorer.notes.find((n) => n.id === inlineInput.id);
        if (note) {
          await explorer.renameNote(note, value);
        }
      }
    }
    setInlineInput(null);
  };

  return (
    <div className="flex h-full flex-col overflow-hidden rounded-md border border-border bg-card text-card-foreground">
      <div className="flex flex-col border-b border-border">
        <button
          type="button"
          onClick={() => setIsVaultScopeOpen(!isVaultScopeOpen)}
          className="flex items-center justify-between p-2 text-xs font-semibold uppercase text-muted-foreground hover:bg-muted hover:text-foreground"
        >
          <span>Vault Scope</span>
          {isVaultScopeOpen ? (
            <ChevronDown size={14} />
          ) : (
            <ChevronRight size={14} />
          )}
        </button>

        {isVaultScopeOpen && (
          <div className="flex flex-col gap-2 p-2 pt-0">
            <BucketSelector
              buckets={explorer.buckets}
              value={explorer.selectedBucketId}
              onChange={(val) => explorer.setSelectedBucketId(val ?? "")}
            />

            {explorer.selectedBucketId && (
              <div className="flex items-center gap-1">
                <Button
                  variant="ghost"
                  className="h-7 flex-1 px-2 text-xs"
                  onClick={() =>
                    setInlineInput({
                      type: "createFile",
                      parentFolderId: explorer.selectedFolderId,
                    })
                  }
                >
                  <Plus size={14} className="mr-1" />
                  New File
                </Button>
                <Button
                  variant="ghost"
                  className="h-7 flex-1 px-2 text-xs"
                  onClick={() =>
                    setInlineInput({
                      type: "createFolder",
                      parentFolderId: explorer.selectedFolderId,
                    })
                  }
                >
                  <FolderPlus size={14} className="mr-1" />
                  New Folder
                </Button>
              </div>
            )}
          </div>
        )}
      </div>

      <div
        className={cn(
          "flex-1 overflow-y-auto p-2 transition-colors",
          isRootDropActive && "bg-primary/10 ring-2 ring-inset ring-primary/40",
        )}
        onClick={() => explorer.selectFolder(null)} // Click empty space to deselect
        onContextMenu={(e) => {
          e.preventDefault();
          if (e.target === e.currentTarget) {
            setContextMenu({
              x: e.clientX,
              y: e.clientY,
              type: "root",
              id: null,
              name: null,
            });
          }
        }}
        onDragEnter={(e) => {
          e.preventDefault();
        }}
        onDragOver={(e) => {
          e.preventDefault();
          e.dataTransfer.dropEffect = "move";
          setIsRootDropActive(true);
        }}
        onDragLeave={() => setIsRootDropActive(false)}
        onDrop={handleRootDrop}
      >
        {!explorer.selectedBucketId ? (
          <div className="p-4 text-center text-sm text-muted-foreground">
            Select a vault to explore
          </div>
        ) : explorer.isLoading ? (
          <div className="flex items-center justify-center p-4">
            <Loader2 size={24} className="animate-spin text-muted-foreground" />
          </div>
        ) : explorer.error ? (
          <div className="p-4 text-center text-sm text-red-500">
            {explorer.error}
          </div>
        ) : (
          <ExplorerNode
            folderId={null}
            folders={explorer.folders}
            notes={explorer.notes}
            expandedFolders={explorer.expandedFolders}
            selectedFolderId={explorer.selectedFolderId}
            selectedNoteId={explorer.selectedNoteId}
            onToggleFolder={explorer.toggleFolder}
            onSelectFolder={explorer.selectFolder}
            onSelectNote={explorer.selectNote}
            onDoubleClickNote={onOpenNoteInNewTab}
            onMoveItem={(type, id, targetId) => {
              if (type === "note")
                void explorer.moveNote(id, explorer.selectedBucketId, targetId);
              if (type === "folder")
                void explorer.moveFolder(
                  id,
                  explorer.selectedBucketId,
                  targetId,
                );
            }}
            onContextMenu={(e, type, id, name) => {
              e.preventDefault();
              setContextMenu({ x: e.clientX, y: e.clientY, type, id, name });
            }}
            inlineInput={inlineInput}
            onInlineSubmit={handleInlineSubmit}
            onInlineCancel={() => setInlineInput(null)}
            depth={0}
          />
        )}
      </div>

      {contextMenu && (
        <div
          className="fixed z-50 min-w-40 overflow-hidden rounded-md border border-border bg-card p-1 text-card-foreground shadow-lg"
          style={{ top: contextMenu.y, left: contextMenu.x }}
          onClick={(e) => e.stopPropagation()}
        >
          {(contextMenu.type === "folder" || contextMenu.type === "root") && (
            <>
              <button
                className="flex w-full items-center gap-2 rounded-sm px-2 py-1.5 text-sm text-muted-foreground hover:bg-muted hover:text-foreground"
                onClick={() => {
                  setInlineInput({
                    type: "createFile",
                    parentFolderId: contextMenu.id,
                  });
                  if (contextMenu.id) {
                    if (!explorer.expandedFolders.has(contextMenu.id)) {
                      explorer.toggleFolder(contextMenu.id);
                    }
                  }
                  setContextMenu(null);
                }}
              >
                <Plus size={14} /> New File
              </button>
              <button
                className="flex w-full items-center gap-2 rounded-sm px-2 py-1.5 text-sm text-muted-foreground hover:bg-muted hover:text-foreground"
                onClick={() => {
                  setInlineInput({
                    type: "createFolder",
                    parentFolderId: contextMenu.id,
                  });
                  if (contextMenu.id) {
                    if (!explorer.expandedFolders.has(contextMenu.id)) {
                      explorer.toggleFolder(contextMenu.id);
                    }
                  }
                  setContextMenu(null);
                }}
              >
                <FolderPlus size={14} /> New Folder
              </button>
              {contextMenu.type === "folder" && (
                <div className="my-1 h-px bg-border" />
              )}
            </>
          )}

          {contextMenu.type === "root" && (
            <button
              className="flex w-full items-center gap-2 rounded-sm px-2 py-1.5 text-sm text-muted-foreground hover:bg-muted hover:text-foreground"
              onClick={() => {
                void explorer.refetchTree();
                setContextMenu(null);
              }}
            >
              Refresh
            </button>
          )}

          {contextMenu.type !== "root" &&
            contextMenu.id &&
            contextMenu.name && (
              <>
                {contextMenu.type === "note" && (
                  <>
                    <button
                      className="flex w-full items-center gap-2 rounded-sm px-2 py-1.5 text-sm text-muted-foreground hover:bg-muted hover:text-foreground"
                      onClick={() => {
                        onOpenNote?.(contextMenu.id!);
                        setContextMenu(null);
                      }}
                    >
                      Open
                    </button>
                    <button
                      className="flex w-full items-center gap-2 rounded-sm px-2 py-1.5 text-sm text-muted-foreground hover:bg-muted hover:text-foreground"
                      onClick={() => {
                        onOpenNoteInNewTab?.(contextMenu.id!);
                        setContextMenu(null);
                      }}
                    >
                      Open in New Tab
                    </button>
                    <button
                      className="flex w-full items-center gap-2 rounded-sm px-2 py-1.5 text-sm text-muted-foreground hover:bg-muted hover:text-foreground"
                      onClick={() => {
                        onShowProperties?.(contextMenu.id!);
                        setContextMenu(null);
                      }}
                    >
                      Properties
                    </button>
                    <div className="my-1 h-px bg-border" />
                  </>
                )}
                <button
                  className="flex w-full items-center gap-2 rounded-sm px-2 py-1.5 text-sm text-muted-foreground hover:bg-muted hover:text-foreground"
                  onClick={() => {
                    setInlineInput({
                      type: "rename",
                      itemType: contextMenu.type as "note" | "folder",
                      id: contextMenu.id!,
                      initialName: contextMenu.name!,
                    });
                    setContextMenu(null);
                  }}
                >
                  Rename
                </button>
                <button
                  className="flex w-full items-center gap-2 rounded-sm px-2 py-1.5 text-sm text-muted-foreground hover:bg-muted hover:text-foreground"
                  onClick={() => {
                    setTargetBucketId(explorer.selectedBucketId);
                    setMoveTarget({
                      type: contextMenu.type as "note" | "folder",
                      id: contextMenu.id!,
                      name: contextMenu.name!,
                    });
                    setContextMenu(null);
                  }}
                >
                  <Move size={14} /> Move to Vault...
                </button>
                <div className="my-1 h-px bg-border" />
                <button
                  className="flex w-full items-center gap-2 rounded-sm px-2 py-1.5 text-sm text-red-600 hover:bg-red-50 dark:text-red-500 dark:hover:bg-red-950/30"
                  onClick={() => {
                    setDeleteTarget({
                      type: contextMenu.type as "note" | "folder",
                      id: contextMenu.id!,
                      name: contextMenu.name!,
                    });
                    setContextMenu(null);
                  }}
                >
                  <Trash2 size={14} /> Delete
                </button>
              </>
            )}
        </div>
      )}

      <Modal
        title="Move to another Vault"
        description={`Move ${moveTarget?.name} to a different vault.`}
        open={Boolean(moveTarget)}
        onClose={() => setMoveTarget(null)}
      >
        <div className="space-y-4">
          <BucketSelector
            buckets={explorer.buckets}
            value={targetBucketId}
            onChange={(val) => setTargetBucketId(val ?? "")}
          />
          <div className="flex justify-end gap-2">
            <Button variant="secondary" onClick={() => setMoveTarget(null)}>
              Cancel
            </Button>
            <Button
              onClick={() => {
                if (!moveTarget) return;
                if (moveTarget.type === "note") {
                  void explorer.moveNote(moveTarget.id, targetBucketId, null);
                } else {
                  void explorer.moveFolder(moveTarget.id, targetBucketId, null);
                }
                setMoveTarget(null);
              }}
              disabled={
                explorer.isMovingNote ||
                explorer.isMovingFolder ||
                targetBucketId === explorer.selectedBucketId
              }
            >
              Move
            </Button>
          </div>
        </div>
      </Modal>

      <ConfirmDialog
        open={Boolean(deleteTarget)}
        title="Delete Item"
        description={`Are you sure you want to delete ${deleteTarget?.name}?`}
        confirmLabel="Delete"
        isPending={explorer.isDeletingNote || explorer.isDeletingFolder}
        onConfirm={() => {
          if (!deleteTarget) return;
          if (deleteTarget.type === "note") {
            void explorer.deleteNote(deleteTarget.id);
          } else {
            void explorer.deleteFolder(deleteTarget.id);
          }
          setDeleteTarget(null);
        }}
        onClose={() => setDeleteTarget(null)}
      />
    </div>
  );
}
