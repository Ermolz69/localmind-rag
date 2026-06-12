import { ChevronDown, ChevronRight, FileText, Folder } from "lucide-react";
import type { NoteDto } from "@entities/note";
import type { Schema } from "@shared/contracts";
import { cn } from "@shared/lib/cn";

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
  onMoveItem,
  onContextMenu,
  depth = 0,
}: ExplorerNodeProps) {
  const childFolders = folders
    .filter((f) => f.parentFolderId === folderId)
    .sort((a, b) => a.name.localeCompare(b.name));

  const childNotes = notes
    .filter((n) => n.folderId === folderId)
    .sort((a, b) => a.title.localeCompare(b.title));

  const paddingLeft = depth * 12 + 8;

  return (
    <div className="flex flex-col">
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
                e.dataTransfer.setData(
                  "text/plain",
                  JSON.stringify({ type: "folder", id: folder.id }),
                );
              }}
              onDragOver={(e) => {
                e.preventDefault();
                e.stopPropagation();
              }}
              onDrop={(e) => {
                e.preventDefault();
                e.stopPropagation();
                const dataStr = e.dataTransfer.getData("text/plain");
                if (!dataStr) return;
                try {
                  const data = JSON.parse(dataStr);
                  if (data.id === folder.id) return; // Cannot drop on itself
                  onMoveItem?.(data.type, data.id, folder.id);
                } catch (err) {
                  console.debug("Drop parse error", err);
                }
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
              <span className="truncate">{folder.name}</span>
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
                onMoveItem={onMoveItem}
                onContextMenu={onContextMenu}
                depth={depth + 1}
              />
            )}
          </div>
        );
      })}

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
            )}
            style={{ paddingLeft: `${paddingLeft + 22}px` }}
            onClick={(e) => {
              e.stopPropagation();
              onSelectNote(note.id);
            }}
            draggable
            onDragStart={(e) => {
              e.stopPropagation();
              e.dataTransfer.setData(
                "text/plain",
                JSON.stringify({ type: "note", id: note.id }),
              );
            }}
            onContextMenu={(e) => {
              onContextMenu?.(e, "note", note.id, note.title);
            }}
          >
            <FileText size={14} className="shrink-0 text-neutral-500" />
            <span className="truncate">{note.title}</span>
          </button>
        );
      })}
    </div>
  );
}
