import { Eye, FileText, Pencil, Trash2 } from "lucide-react";
import type { BucketDto } from "@entities/bucket";
import type { NoteDraft } from "../model/types";
import { Button, Input, Textarea } from "@shared/ui";
import { BucketSelector } from "./BucketSelector";

type NoteEditorProps = {
  buckets: BucketDto[];
  draft: NoteDraft;
  isDirty: boolean;
  isSubmitting: boolean;
  onDraftChange: (draft: NoteDraft) => void;
  onSave: () => void;
  onCancel: () => void;
  onDelete: () => void;
};

export function NoteEditor({
  buckets,
  draft,
  isDirty,
  isSubmitting,
  onDraftChange,
  onSave,
  onCancel,
  onDelete,
}: NoteEditorProps) {
  return (
    <div className="grid h-full min-h-[38rem] overflow-hidden rounded-xl border border-border bg-card shadow-sm xl:grid-cols-[minmax(0,1fr)_18rem]">
      <div className="flex min-w-0 flex-col">
        <div className="border-b border-border px-6 py-5">
          <Input
            className="h-auto border-0 bg-transparent px-0 text-2xl font-semibold shadow-none focus:ring-0"
            value={draft.title}
            onChange={(event) =>
              onDraftChange({ ...draft, title: event.target.value })
            }
            placeholder="Untitled"
          />
          <div className="mt-3 flex flex-wrap items-center gap-2 text-xs text-muted-foreground">
            <span className="inline-flex items-center gap-1 rounded-md bg-muted px-2 py-1">
              <Pencil size={13} aria-hidden />
              Markdown
            </span>
            {isDirty ? (
              <span className="rounded-md bg-muted px-2 py-1">
                Unsaved changes
              </span>
            ) : (
              <span className="rounded-md bg-muted px-2 py-1">Saved</span>
            )}
          </div>
        </div>

        <div className="min-h-0 flex-1">
          <Textarea
            className="h-full min-h-[30rem] resize-none border-0 bg-card px-6 py-5 font-mono text-[15px] leading-7 shadow-none focus:ring-0"
            value={draft.markdown}
            onChange={(event) =>
              onDraftChange({ ...draft, markdown: event.target.value })
            }
            placeholder="Start writing..."
          />
        </div>
      </div>

      <aside className="flex flex-col gap-4 border-t border-border bg-background p-5 xl:border-l xl:border-t-0">
        <div>
          <h2 className="flex items-center gap-2 text-sm font-semibold text-card-foreground">
            <FileText size={16} aria-hidden />
            Properties
          </h2>
          <p className="mt-1 text-xs leading-5 text-muted-foreground">
            Organize this note inside a local folder.
          </p>
        </div>

        <BucketSelector
          buckets={buckets}
          disabled
          value={draft.bucketId}
          onChange={(bucketId) => onDraftChange({ ...draft, bucketId })}
        />
        <p className="-mt-2 text-xs text-muted-foreground">
          The current API does not support moving an existing note.
        </p>

        <div className="rounded-md border border-border bg-card p-3 text-xs leading-5 text-muted-foreground">
          <div className="mb-2 flex items-center gap-2 font-medium text-foreground">
            <Eye size={14} aria-hidden />
            Source-ready markdown
          </div>
          Write local markdown now. Backlinks, graph view, and document
          references can plug into this panel later.
        </div>

        <div className="mt-auto flex flex-col gap-2">
          <Button onClick={onSave} disabled={isSubmitting || !isDirty}>
            {isSubmitting ? "Saving..." : "Save note"}
          </Button>
          <Button variant="secondary" onClick={onCancel} disabled={!isDirty}>
            Cancel changes
          </Button>
          <Button variant="secondary" onClick={onDelete}>
            <Trash2 size={16} aria-hidden />
            Delete note
          </Button>
        </div>
      </aside>
    </div>
  );
}
