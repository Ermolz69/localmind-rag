import { ChevronDown, ChevronRight, FileText, Folder } from "lucide-react";
import { useState } from "react";
import type { NoteDto } from "@entities/note";
import type { Schema } from "@shared/contracts";
import { cn } from "@shared/lib/cn";
import {
  getVaultItemDragData,
  setVaultItemDragData,
} from "../model/dragPayload";
import { InlineExplorerInput } from "./InlineExplorerInput";

type NoteFolderDto = Schema<"NoteFolderDto">;

interface ExplorerNodeProps {
  folderId: string | null;
  folders: NoteFolderDto[];
  notes: NoteDto[];
  expandedFolders: Set<string>;
  selectedFolderId: string | null;
  selectedNoteId: string | null;
  onToggleFolder: (folderId: string) => void;
  onSelectFolder: (folderId: string | null) => void;
  onSelectNote: (noteId: string) => void;
  onDoubleClickNote?: (noteId: string) => void;
  onMoveItem?: (
    type: "note" | "folder",
    id: string,
    targetFolderId: string | null,
  ) => void;
  onContextMenu?: (
    e: React.MouseEvent,
    type: "note" | "folder",
    id: string,
    name: string,
  ) => void;
  inlineInput?:
    | { type: "createFile"; parentFolderId: string | null }
    | { type: "createFolder"; parentFolderId: string | null }
    | {
        type: "rename";
        itemType: "note" | "folder";
        id: string;
        initialName: string;
      }
    | null;
  onInlineSubmit?: (value: string) => void;
  onInlineCancel?: () => void;
  depth?: number;
}

export function ExplorerNode({
  folderId,
  folders,
  notes,
  expandedFolders,
  selectedFolderId,
  selectedNoteId,
  onToggleFolder,
  onSelectFolder,
  onSelectNote,
  onDoubleClickNote,
  onMoveItem,
  onContextMenu,
  inlineInput,
  onInlineSubmit,
  onInlineCancel,
  depth = 0,
}: ExplorerNodeProps) {
  const [activeDropFolderId, setActiveDropFolderId] = useState<string | null>(
    null,
  );
  const [activeDropNoteId, setActiveDropNoteId] = useState<string | null>(null);
  const childFolders = folders
    .filter((f) => f.parentFolderId === folderId)
    .sort((a, b) => a.name.localeCompare(b.name));

  const childNotes = notes
    .filter((n) => n.folderId === folderId)
    .sort((a, b) => a.title.localeCompare(b.title));

  const paddingLeft = depth * 12 + 8;

  return (
    <div
      className="flex flex-col"
      onDragEnter={(e) => {
        e.preventDefault();
        e.stopPropagation();
      }}
      onDragOver={(e) => {
        e.preventDefault();
        e.stopPropagation();
        e.dataTransfer.dropEffect = "move";
      }}
      onDrop={(e) => {
        e.preventDefault();
        e.stopPropagation();
        const data = getVaultItemDragData(e.dataTransfer);
        if (!data) {
          return;
        }
        if (data.type === "folder" && data.id === folderId) {
          return;
        }
        onMoveItem?.(data.type, data.id, folderId);
      }}
    >
      {inlineInput?.type === "createFolder" &&
        inlineInput.parentFolderId === folderId && (
          <InlineExplorerInput
            type="folder"
            paddingLeft={paddingLeft}
            onSubmit={onInlineSubmit ?? (() => {})}
            onCancel={onInlineCancel ?? (() => {})}
          />
        )}
      {childFolders.map((folder) => {
        const isExpanded = expandedFolders.has(folder.id);
        const isSelected = selectedFolderId === folder.id;

        return (
          <div key={folder.id} className="flex flex-col">
            <button
              type="button"
              className={cn(
                "flex items-center gap-1.5 rounded-sm px-2 py-1 text-sm transition-colors hover:bg-neutral-200 dark:hover:bg-neutral-800",
                isSelected &&
                  "bg-blue-100 text-blue-900 dark:bg-blue-900/50 dark:text-blue-100",
                activeDropFolderId === folder.id &&
                  "bg-primary/10 ring-2 ring-inset ring-primary/40",
              )}
              style={{ paddingLeft: `${paddingLeft}px` }}
              onClick={(e) => {
                e.stopPropagation();
                onSelectFolder(folder.id);
              }}
              onDoubleClick={(e) => {
                e.stopPropagation();
                onToggleFolder(folder.id);
              }}
              draggable
              onDragStart={(e) => {
                e.stopPropagation();
                setVaultItemDragData(e.dataTransfer, {
                  type: "folder",
                  id: folder.id,
                });
              }}
              onDragEnter={(e) => {
                e.preventDefault();
                e.stopPropagation();
              }}
              onDragOver={(e) => {
                e.preventDefault();
                e.stopPropagation();
                e.dataTransfer.dropEffect = "move";
                setActiveDropFolderId(folder.id);
              }}
              onDragLeave={() => {
                setActiveDropFolderId((current) =>
                  current === folder.id ? null : current,
                );
              }}
              onDrop={(e) => {
                e.preventDefault();
                e.stopPropagation();
                setActiveDropFolderId(null);
                const data = getVaultItemDragData(e.dataTransfer);
                if (!data || data.id === folder.id) {
                  return;
                }
                onMoveItem?.(data.type, data.id, folder.id);
              }}
              onContextMenu={(e) => {
                onContextMenu?.(e, "folder", folder.id, folder.name);
              }}
            >
              <span
                className="flex h-4 w-4 shrink-0 cursor-pointer items-center justify-center text-neutral-500 hover:text-neutral-700 dark:hover:text-neutral-300"
                onClick={(e) => {
                  e.stopPropagation();
                  onToggleFolder(folder.id);
                }}
              >
                {isExpanded ? (
                  <ChevronDown size={14} />
                ) : (
                  <ChevronRight size={14} />
                )}
              </span>
              <Folder size={14} className="shrink-0 text-blue-500" />
              {inlineInput?.type === "rename" &&
              inlineInput.itemType === "folder" &&
              inlineInput.id === folder.id ? (
                <div
                  className="flex-1"
                  onClick={(e) => e.stopPropagation()}
                  onDoubleClick={(e) => e.stopPropagation()}
                >
                  <InlineExplorerInput
                    type="folder"
                    initialValue={inlineInput.initialName}
                    isRename={true}
                    onSubmit={onInlineSubmit ?? (() => {})}
                    onCancel={onInlineCancel ?? (() => {})}
                  />
                </div>
              ) : (
                <span className="truncate">{folder.name}</span>
              )}
            </button>

            {isExpanded && (
              <ExplorerNode
                folderId={folder.id}
                folders={folders}
                notes={notes}
                expandedFolders={expandedFolders}
                selectedFolderId={selectedFolderId}
                selectedNoteId={selectedNoteId}
                onToggleFolder={onToggleFolder}
                onSelectFolder={onSelectFolder}
                onSelectNote={onSelectNote}
                onDoubleClickNote={onDoubleClickNote}
                onMoveItem={onMoveItem}
                onContextMenu={onContextMenu}
                inlineInput={inlineInput}
                onInlineSubmit={onInlineSubmit}
                onInlineCancel={onInlineCancel}
                depth={depth + 1}
              />
            )}
          </div>
        );
      })}

      {inlineInput?.type === "createFile" &&
        inlineInput.parentFolderId === folderId && (
          <InlineExplorerInput
            type="note"
            paddingLeft={paddingLeft + 22}
            onSubmit={onInlineSubmit ?? (() => {})}
            onCancel={onInlineCancel ?? (() => {})}
          />
        )}

      {childNotes.map((note) => {
        const isSelected = selectedNoteId === note.id;

        return (
          <button
            key={note.id}
            type="button"
            className={cn(
              "flex items-center gap-1.5 rounded-sm px-2 py-1 text-sm transition-colors hover:bg-neutral-200 dark:hover:bg-neutral-800",
              isSelected &&
                "bg-blue-100 text-blue-900 dark:bg-blue-900/50 dark:text-blue-100",
              activeDropNoteId === note.id &&
                "bg-primary/10 ring-2 ring-inset ring-primary/40",
            )}
            style={{ paddingLeft: `${paddingLeft + 22}px` }}
            onClick={(e) => {
              e.stopPropagation();
              onSelectNote(note.id);
            }}
            onDoubleClick={(e) => {
              e.stopPropagation();
              onDoubleClickNote?.(note.id);
            }}
            draggable
            onDragStart={(e) => {
              e.stopPropagation();
              setVaultItemDragData(e.dataTransfer, {
                type: "note",
                id: note.id,
              });
            }}
            onDragEnter={(e) => {
              e.preventDefault();
              e.stopPropagation();
            }}
            onDragOver={(e) => {
              e.preventDefault();
              e.stopPropagation();
              e.dataTransfer.dropEffect = "move";
              setActiveDropNoteId(note.id);
            }}
            onDragLeave={() => {
              setActiveDropNoteId((current) =>
                current === note.id ? null : current,
              );
            }}
            onDrop={(e) => {
              e.preventDefault();
              e.stopPropagation();
              setActiveDropNoteId(null);
              const data = getVaultItemDragData(e.dataTransfer);
              if (!data || data.id === note.id) {
                return;
              }
              onMoveItem?.(data.type, data.id, folderId);
            }}
            onContextMenu={(e) => {
              onContextMenu?.(e, "note", note.id, note.title);
            }}
          >
            <FileText size={14} className="shrink-0 text-neutral-500" />
            {inlineInput?.type === "rename" &&
            inlineInput.itemType === "note" &&
            inlineInput.id === note.id ? (
              <div
                className="flex-1"
                onClick={(e) => e.stopPropagation()}
                onDoubleClick={(e) => e.stopPropagation()}
              >
                <InlineExplorerInput
                  type="note"
                  initialValue={inlineInput.initialName}
                  isRename={true}
                  onSubmit={onInlineSubmit ?? (() => {})}
                  onCancel={onInlineCancel ?? (() => {})}
                />
              </div>
            ) : (
              <span className="truncate">{note.title}</span>
            )}
          </button>
        );
      })}
    </div>
  );
}
