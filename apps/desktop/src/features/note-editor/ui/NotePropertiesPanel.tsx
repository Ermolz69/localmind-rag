import { X, Trash2, FileText } from "lucide-react";
import type { NoteDto } from "@entities/note";
import type { BucketDto } from "@entities/bucket";
import { BucketSelector } from "./BucketSelector";
import { Button } from "@shared/ui";

type NotePropertiesPanelProps = {
  note: NoteDto;
  buckets: BucketDto[];
  isOpen: boolean;
  onClose: () => void;
  onDelete: () => void;
};

export function NotePropertiesPanel({
  note,
  buckets,
  isOpen,
  onClose,
  onDelete,
}: NotePropertiesPanelProps) {
  if (!isOpen) return null;

  return (
    <div className="absolute inset-y-0 right-0 z-20 flex w-72 flex-col border-l border-border bg-card shadow-2xl slide-in-from-right-full animate-in duration-200">
      <div className="flex items-center justify-between border-b border-border px-4 py-3">
        <h2 className="flex items-center gap-2 text-sm font-semibold text-card-foreground">
          <FileText size={16} aria-hidden />
          Properties
        </h2>
        <button
          className="rounded-sm p-1 text-muted-foreground hover:bg-muted hover:text-foreground"
          onClick={onClose}
        >
          <X size={16} />
        </button>
      </div>

      <div className="flex flex-1 flex-col gap-5 overflow-y-auto p-4">
        <div>
          <label className="text-xs font-medium text-muted-foreground">Title</label>
          <p className="mt-1 break-words text-sm font-medium text-foreground">
            {note.title}
          </p>
        </div>

        <div>
          <label className="text-xs font-medium text-muted-foreground">Location</label>
          <div className="mt-1">
            <BucketSelector
              buckets={buckets}
              disabled
              value={note.bucketId}
              onChange={() => {}}
            />
          </div>
          <p className="mt-1 text-xs text-muted-foreground">
            Use the explorer context menu to move notes.
          </p>
        </div>

        <div>
          <label className="text-xs font-medium text-muted-foreground">Created</label>
          <p className="mt-1 text-sm text-foreground">
            {new Date(note.createdAt).toLocaleString()}
          </p>
        </div>

        <div>
          <label className="text-xs font-medium text-muted-foreground">Last modified</label>
          <p className="mt-1 text-sm text-foreground">
            {note.updatedAt ? new Date(note.updatedAt).toLocaleString() : "Never"}
          </p>
        </div>

        <div className="mt-auto pt-4">
          <Button
            variant="secondary"
            onClick={onDelete}
            className="w-full text-red-600 hover:text-red-700 dark:text-red-500 dark:hover:text-red-400"
          >
            <Trash2 size={16} className="mr-2" />
            Delete note
          </Button>
        </div>
      </div>
    </div>
  );
}
