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
import { Button, ConfirmDialog, Input, Modal } from "@shared/ui";
import { cn } from "@shared/lib/cn";
import { getVaultItemDragData } from "../model/dragPayload";
import { useVaultExplorer } from "../model/useVaultExplorer";
import { ExplorerNode } from "./ExplorerNode";

export function VaultExplorer({
  explorer,
  onCreateFile,
}: {
  explorer: ReturnType<typeof useVaultExplorer>;
  onCreateFile: () => void;
}) {
  const [isPropertiesOpen, setIsPropertiesOpen] = useState(true);
  const [isCreateFolderOpen, setIsCreateFolderOpen] = useState(false);
  const [newFolderName, setNewFolderName] = useState("");
  const [isRootDropActive, setIsRootDropActive] = useState(false);
  const [contextMenu, setContextMenu] = useState<{
    x: number;
    y: number;
    type: "note" | "folder";
    id: string;
    name: string;
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

  const createFolder = async () => {
    const created = await explorer.createFolder(newFolderName);
    if (created) {
      setNewFolderName("");
      setIsCreateFolderOpen(false);
    }
  };

  return (
    <div className="flex h-full flex-col overflow-hidden rounded-md border border-neutral-200 bg-neutral-50 dark:border-neutral-800 dark:bg-neutral-900/50">
      <div className="flex flex-col border-b border-neutral-200 dark:border-neutral-800">
        <button
          type="button"
          onClick={() => setIsPropertiesOpen(!isPropertiesOpen)}
          className="flex items-center justify-between p-2 text-xs font-semibold uppercase text-neutral-500 hover:bg-neutral-100 dark:hover:bg-neutral-800"
        >
          <span>Vault Scope</span>
          {isPropertiesOpen ? (
            <ChevronDown size={14} />
          ) : (
            <ChevronRight size={14} />
          )}
        </button>

        {isPropertiesOpen && (
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
                  onClick={onCreateFile}
                >
                  <Plus size={14} className="mr-1" />
                  New File
                </Button>
                <Button
                  variant="ghost"
                  className="h-7 flex-1 px-2 text-xs"
                  onClick={() => setIsCreateFolderOpen(true)}
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
        onDragOver={(e) => {
          e.preventDefault();
          e.dataTransfer.dropEffect = "move";
          setIsRootDropActive(true);
        }}
        onDragLeave={() => setIsRootDropActive(false)}
        onDrop={handleRootDrop}
      >
        {!explorer.selectedBucketId ? (
          <div className="p-4 text-center text-sm text-neutral-500">
            Select a vault to explore
          </div>
        ) : explorer.isLoading ? (
          <div className="flex items-center justify-center p-4">
            <Loader2 size={24} className="animate-spin text-neutral-500" />
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
            depth={0}
          />
        )}
      </div>

      {contextMenu && (
        <div
          className="fixed z-50 min-w-40 overflow-hidden rounded-md border border-neutral-200 bg-white p-1 shadow-lg dark:border-neutral-800 dark:bg-neutral-900"
          style={{ top: contextMenu.y, left: contextMenu.x }}
          onClick={(e) => e.stopPropagation()}
        >
          <button
            className="flex w-full items-center gap-2 rounded-sm px-2 py-1.5 text-sm text-neutral-700 hover:bg-neutral-100 dark:text-neutral-300 dark:hover:bg-neutral-800"
            onClick={() => {
              setTargetBucketId(explorer.selectedBucketId);
              setMoveTarget({
                type: contextMenu.type,
                id: contextMenu.id,
                name: contextMenu.name,
              });
              setContextMenu(null);
            }}
          >
            <Move size={14} /> Move to Vault...
          </button>
          <button
            className="flex w-full items-center gap-2 rounded-sm px-2 py-1.5 text-sm text-red-600 hover:bg-red-50 dark:text-red-500 dark:hover:bg-red-950/30"
            onClick={() => {
              setDeleteTarget({
                type: contextMenu.type,
                id: contextMenu.id,
                name: contextMenu.name,
              });
              setContextMenu(null);
            }}
          >
            <Trash2 size={14} /> Delete
          </button>
        </div>
      )}

      <Modal
        title="Create folder"
        description={
          explorer.lastSelectedFolderId
            ? "Add a folder inside the last selected folder."
            : "Add a folder at the root of this vault."
        }
        open={isCreateFolderOpen}
        onClose={() => {
          setIsCreateFolderOpen(false);
          setNewFolderName("");
        }}
      >
        <form
          className="space-y-4"
          onSubmit={(event) => {
            event.preventDefault();
            void createFolder();
          }}
        >
          <div>
            <label
              className="mb-2 block text-sm font-medium"
              htmlFor="folder-name"
            >
              Folder name
            </label>
            <Input
              id="folder-name"
              value={newFolderName}
              onChange={(event) => setNewFolderName(event.target.value)}
              placeholder="New folder"
              autoFocus
            />
          </div>
          <div className="flex justify-end gap-2">
            <Button
              type="button"
              variant="secondary"
              onClick={() => {
                setIsCreateFolderOpen(false);
                setNewFolderName("");
              }}
            >
              Cancel
            </Button>
            <Button
              type="submit"
              disabled={explorer.isCreatingFolder || !newFolderName.trim()}
            >
              {explorer.isCreatingFolder ? "Creating..." : "Create folder"}
            </Button>
          </div>
        </form>
      </Modal>

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
