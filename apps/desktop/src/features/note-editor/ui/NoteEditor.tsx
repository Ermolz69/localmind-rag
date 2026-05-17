import { Trash2 } from "lucide-react";
import type { BucketDto } from "@entities/bucket";
import type { NoteDraft } from "../model/types";
import { Button } from "@shared/ui";
import { Input } from "@shared/ui";
import { Textarea } from "@shared/ui";
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
    <div className="space-y-4 rounded-md border border-border bg-card p-6">
      <div className="space-y-3">
        <div>
          <label className="mb-2 block text-sm font-medium">Title</label>
          <Input
            value={draft.title}
            onChange={(event) =>
              onDraftChange({ ...draft, title: event.target.value })
            }
            placeholder="Note title"
          />
        </div>
        <BucketSelector
          buckets={buckets}
          value={draft.bucketId}
          onChange={(bucketId) => onDraftChange({ ...draft, bucketId })}
        />
        <div>
          <label className="mb-2 block text-sm font-medium">Markdown</label>
          <Textarea
            className="min-h-96 font-mono"
            value={draft.markdown}
            onChange={(event) =>
              onDraftChange({ ...draft, markdown: event.target.value })
            }
            placeholder="Write markdown here..."
          />
        </div>
      </div>
      <div className="flex flex-wrap gap-2">
        <Button onClick={onSave} disabled={isSubmitting || !isDirty}>
          {isSubmitting ? "Saving..." : "Save"}
        </Button>
        <Button variant="secondary" onClick={onCancel} disabled={!isDirty}>
          Cancel
        </Button>
        <Button variant="secondary" onClick={onDelete}>
          <Trash2 size={16} aria-hidden />
          Delete
        </Button>
      </div>
    </div>
  );
}
