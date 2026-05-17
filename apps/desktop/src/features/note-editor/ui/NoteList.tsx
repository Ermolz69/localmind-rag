import { FileText } from "lucide-react";
import type { BucketDto } from "@entities/bucket";
import type { NoteDto } from "@entities/note";
import { Button } from "@shared/ui";
import { EmptyState } from "@shared/ui";
import { cn } from "@shared/lib/cn";

type NoteListProps = {
  notes: NoteDto[];
  buckets: BucketDto[];
  selectedNoteId: string | null;
  isLoading: boolean;
  hasMore: boolean;
  isLoadingMore: boolean;
  onSelect: (noteId: string) => void;
  onLoadMore: () => void;
};

export function NoteList({
  notes,
  buckets,
  selectedNoteId,
  isLoading,
  hasMore,
  isLoadingMore,
  onSelect,
  onLoadMore,
}: NoteListProps) {
  if (isLoading) {
    return (
      <div className="rounded-md border border-border bg-card p-4 text-sm text-muted-foreground">
        Loading notes...
      </div>
    );
  }

  if (notes.length === 0) {
    return (
      <EmptyState
        icon={<FileText size={18} aria-hidden />}
        title="No notes yet"
        description="Create a note to start writing local markdown."
      />
    );
  }

  return (
    <div className="space-y-2">
      <div className="space-y-1 rounded-md border border-border bg-card p-2">
        {notes.map((note) => (
          <button
            key={note.id}
            className={cn(
              "w-full rounded-md px-3 py-2 text-left text-sm transition",
              selectedNoteId === note.id
                ? "bg-primary text-primary-foreground"
                : "text-foreground hover:bg-muted",
            )}
            onClick={() => onSelect(note.id)}
          >
            <div className="truncate font-medium">{note.title}</div>
            {note.bucketId ? (
              <div className="text-xs opacity-75">
                {buckets.find((bucket) => bucket.id === note.bucketId)?.name}
              </div>
            ) : null}
          </button>
        ))}
      </div>
      {hasMore ? (
        <Button
          className="w-full"
          variant="secondary"
          onClick={onLoadMore}
          disabled={isLoadingMore}
        >
          {isLoadingMore ? "Loading..." : "Load more"}
        </Button>
      ) : null}
    </div>
  );
}
