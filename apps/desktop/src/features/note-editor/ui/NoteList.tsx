import {
  ChevronDown,
  ChevronRight,
  FileText,
  FolderClosed,
  Search,
} from "lucide-react";
import { useState } from "react";
import type { BucketDto } from "@entities/bucket";
import type { NoteDto } from "@entities/note";
import { Button, EmptyState, Input } from "@shared/ui";
import { cn } from "@shared/lib/cn";

type NoteListProps = {
  notes: NoteDto[];
  buckets: BucketDto[];
  query: string;
  selectedNoteId: string | null;
  isLoading: boolean;
  hasMore: boolean;
  isLoadingMore: boolean;
  onQueryChange: (query: string) => void;
  onSelect: (noteId: string) => void;
  onLoadMore: () => void;
};

export function NoteList({
  notes,
  buckets,
  query,
  selectedNoteId,
  isLoading,
  hasMore,
  isLoadingMore,
  onQueryChange,
  onSelect,
  onLoadMore,
}: NoteListProps) {
  const [collapsedGroupIds, setCollapsedGroupIds] = useState<Set<string>>(
    () => new Set(),
  );
  const defaultNotes = notes.filter((note) => !note.bucketId);
  const bucketGroups = buckets
    .map((bucket) => ({
      bucket,
      notes: notes.filter((note) => note.bucketId === bucket.id),
    }))
    .filter((group) => group.notes.length > 0);

  const groups = [
    ...(defaultNotes.length > 0
      ? [{ id: "default", name: "Default", notes: defaultNotes }]
      : []),
    ...bucketGroups.map((group) => ({
      id: group.bucket.id,
      name: group.bucket.name,
      notes: group.notes,
    })),
  ];

  function toggleGroup(groupId: string) {
    setCollapsedGroupIds((current) => {
      const next = new Set(current);
      if (next.has(groupId)) {
        next.delete(groupId);
      } else {
        next.add(groupId);
      }

      return next;
    });
  }

  if (isLoading) {
    return (
      <div className="h-full rounded-xl border border-border bg-card p-4 text-sm text-muted-foreground shadow-sm">
        Loading vault...
      </div>
    );
  }

  if (notes.length === 0) {
    return (
      <div className="h-full rounded-xl border border-border bg-card p-3 shadow-sm">
        <VaultSearch value={query} onChange={onQueryChange} />
        <div className="mt-4">
          <EmptyState
            icon={<FileText size={18} aria-hidden />}
            title="No notes yet"
            description="Create a note to start writing local markdown."
          />
        </div>
      </div>
    );
  }

  return (
    <div className="flex h-full min-h-0 flex-col rounded-xl border border-border bg-card shadow-sm">
      <div className="border-b border-border p-3">
        <VaultSearch value={query} onChange={onQueryChange} />
      </div>
      <div className="min-h-0 flex-1 space-y-3 overflow-auto p-3">
        {groups.map((group) => (
          <div key={group.id}>
            <button
              type="button"
              className="mb-1 flex h-8 w-full items-center gap-2 rounded-md px-2 text-left text-xs font-semibold uppercase tracking-wide text-muted-foreground transition-colors hover:bg-muted hover:text-foreground"
              onClick={() => toggleGroup(group.id)}
              aria-expanded={!collapsedGroupIds.has(group.id)}
            >
              {collapsedGroupIds.has(group.id) ? (
                <ChevronRight size={14} aria-hidden />
              ) : (
                <ChevronDown size={14} aria-hidden />
              )}
              <FolderClosed size={14} aria-hidden />
              <span className="truncate">{group.name}</span>
              <span className="ml-auto rounded bg-muted px-1.5 py-0.5 text-[10px] leading-none">
                {group.notes.length}
              </span>
            </button>
            {!collapsedGroupIds.has(group.id) ? (
              <div className="space-y-1">
                {group.notes.map((note) => (
                  <button
                    key={note.id}
                    className={cn(
                      "flex h-9 w-full items-center gap-2 rounded-md px-2 text-left text-sm transition",
                      selectedNoteId === note.id
                        ? "bg-primary text-primary-foreground"
                        : "text-muted-foreground hover:bg-muted hover:text-foreground",
                    )}
                    onClick={() => onSelect(note.id)}
                  >
                    <FileText className="shrink-0" size={15} aria-hidden />
                    <span className="min-w-0 truncate">{note.title}</span>
                  </button>
                ))}
              </div>
            ) : null}
          </div>
        ))}
      </div>
      {hasMore ? (
        <div className="border-t border-border p-3">
          <Button
            className="w-full"
            variant="secondary"
            onClick={onLoadMore}
            disabled={isLoadingMore}
          >
            {isLoadingMore ? "Loading..." : "Load more"}
          </Button>
        </div>
      ) : null}
    </div>
  );
}

function VaultSearch({
  value,
  onChange,
}: {
  value: string;
  onChange: (value: string) => void;
}) {
  return (
    <div className="relative">
      <Search
        className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground"
        size={16}
        aria-hidden
      />
      <Input
        className="h-10 pl-9"
        placeholder="Search files"
        value={value}
        onChange={(event) => onChange(event.target.value)}
      />
    </div>
  );
}
