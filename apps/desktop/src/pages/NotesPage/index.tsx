import { FileText, Plus, Trash2 } from "lucide-react";
import { useCallback, useEffect, useState } from "react";
import {
  localApi,
  type BucketDto,
  type NoteDto,
} from "../../shared/api/client";
import { cn } from "../../shared/lib/cn";
import { Button } from "../../shared/ui/Button";
import { EmptyState } from "../../shared/ui/EmptyState";
import { Modal } from "../../shared/ui/Modal";
import { BucketSelector } from "./BucketSelector";

export function NotesPage() {
  const [notes, setNotes] = useState<NoteDto[]>([]);
  const [buckets, setBuckets] = useState<BucketDto[]>([]);
  const [selectedNoteId, setSelectedNoteId] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isSaving, setIsSaving] = useState(false);

  // Form state for editing
  const [editTitle, setEditTitle] = useState("");
  const [editMarkdown, setEditMarkdown] = useState("");
  const [editBucketId, setEditBucketId] = useState<string | null>(null);

  // Form state for creating new note
  const [newNoteTitle, setNewNoteTitle] = useState("");
  const [newNoteBucketId, setNewNoteBucketId] = useState<string | null>(null);

  const selectedNote = notes.find((note) => note.id === selectedNoteId);

  const loadData = useCallback(async () => {
    setError(null);
    try {
      const [nextNotes, nextBuckets] = await Promise.all([
        localApi.getNotes(),
        localApi.getBuckets(),
      ]);
      setNotes(nextNotes);
      setBuckets(nextBuckets);
    } catch (exception) {
      setError(
        exception instanceof Error
          ? exception.message
          : "Unable to load notes.",
      );
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadData();
  }, [loadData]);

  // Update edit fields when selected note changes
  useEffect(() => {
    if (selectedNote) {
      setEditTitle(selectedNote.title);
      setEditMarkdown(selectedNote.markdown);
      setEditBucketId(selectedNote.bucketId ?? null);
    }
  }, [selectedNote]);

  async function createNote() {
    const title = newNoteTitle.trim();
    if (!title) {
      return;
    }

    setError(null);
    setIsSubmitting(true);
    try {
      const note = await localApi.createNote({
        title,
        markdown: "",
        bucketId: newNoteBucketId,
      });
      setNewNoteTitle("");
      setNewNoteBucketId(null);
      setIsModalOpen(false);
      setNotes((prev) => [...prev, note]);
      setSelectedNoteId(note.id);
    } catch (exception) {
      setError(
        exception instanceof Error
          ? exception.message
          : "Failed to create note.",
      );
    } finally {
      setIsSubmitting(false);
    }
  }

  async function saveNote() {
    if (!selectedNote) {
      return;
    }

    setError(null);
    setIsSaving(true);
    try {
      await localApi.updateNote(selectedNote.id, {
        title: editTitle,
        markdown: editMarkdown,
        bucketId: editBucketId,
      });
      // Update the note in the list
      setNotes((prev) =>
        prev.map((note) =>
          note.id === selectedNote.id
            ? {
                ...note,
                title: editTitle,
                markdown: editMarkdown,
                bucketId: editBucketId ?? null,
              }
            : note,
        ),
      );
    } catch (exception) {
      setError(
        exception instanceof Error ? exception.message : "Failed to save note.",
      );
    } finally {
      setIsSaving(false);
    }
  }

  async function deleteNote(noteId: string) {
    setError(null);
    try {
      await localApi.deleteNote(noteId);
      setNotes((prev) => prev.filter((note) => note.id !== noteId));
      if (selectedNoteId === noteId) {
        setSelectedNoteId(null);
      }
    } catch (exception) {
      setError(
        exception instanceof Error ? exception.message : "Failed to delete note.",
      );
    }
  }

  return (
    <section className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold">Notes</h1>
          <p className="text-sm text-muted-foreground">
            Local markdown notes and future document references.
          </p>
        </div>
        <Button onClick={() => setIsModalOpen(true)}>
          <Plus size={16} aria-hidden />
          New note
        </Button>
      </div>

      {error && (
        <div className="rounded-md border border-red-500/50 bg-red-500/10 p-3 text-sm text-red-600">
          {error}
        </div>
      )}

      <div className="grid gap-4 lg:grid-cols-[minmax(0,300px)_1fr]">
        {/* Notes List */}
        <div className="space-y-2">
          {isLoading ? (
            <div className="rounded-md border border-border bg-card p-4 text-sm text-muted-foreground">
              Loading notes...
            </div>
          ) : notes.length === 0 ? (
            <div className="rounded-md border border-dashed border-border bg-card p-4 text-sm text-muted-foreground">
              No notes yet. Create one to get started.
            </div>
          ) : (
            <div className="space-y-1 rounded-md border border-border bg-card p-2">
              {notes.map((note) => (
                <button
                  key={note.id}
                  className={cn(
                    "w-full text-left rounded-md px-3 py-2 text-sm transition",
                    selectedNoteId === note.id
                      ? "bg-primary text-primary-foreground"
                      : "text-foreground hover:bg-muted",
                  )}
                  onClick={() => setSelectedNoteId(note.id)}
                >
                  <div className="truncate font-medium">{note.title}</div>
                  {note.bucketId && (
                    <div className="text-xs opacity-75">
                      {
                        buckets.find((b) => b.id === note.bucketId)
                          ?.name
                      }
                    </div>
                  )}
                </button>
              ))}
            </div>
          )}
        </div>

        {/* Note Detail / Editor */}
        {selectedNote ? (
          <div className="space-y-4 rounded-md border border-border bg-card p-6">
            <div className="space-y-3">
              <div>
                <label className="mb-2 block text-sm font-medium">Title</label>
                <input
                  className="h-10 w-full rounded-md border border-border bg-background px-3 text-sm text-foreground outline-none"
                  value={editTitle}
                  onChange={(e) => setEditTitle(e.target.value)}
                  placeholder="Note title"
                />
              </div>

              <BucketSelector
                buckets={buckets}
                value={editBucketId}
                onChange={setEditBucketId}
              />

              <div>
                <label className="mb-2 block text-sm font-medium">
                  Markdown
                </label>
                <textarea
                  className="min-h-96 w-full rounded-md border border-border bg-background px-3 py-2 text-sm text-foreground font-mono outline-none"
                  value={editMarkdown}
                  onChange={(e) => setEditMarkdown(e.target.value)}
                  placeholder="Write markdown here..."
                />
              </div>
            </div>

            <div className="flex gap-2">
              <Button
                onClick={() => void saveNote()}
                disabled={isSaving}
              >
                {isSaving ? "Saving..." : "Save"}
              </Button>
              <Button
                variant="secondary"
                onClick={() => void deleteNote(selectedNote.id)}
              >
                <Trash2 size={16} aria-hidden />
                Delete
              </Button>
            </div>
          </div>
        ) : (
          <EmptyState
            icon={<FileText size={20} aria-hidden />}
            title="No note selected"
            description="Select a note from the list or create a new one to get started."
          />
        )}
      </div>

      {/* Create Note Modal */}
      <Modal
        title="Create Note"
        description="Create a new markdown note"
        open={isModalOpen}
        onClose={() => {
          setIsModalOpen(false);
          setNewNoteTitle("");
          setNewNoteBucketId(null);
          setError(null);
        }}
      >
        <div className="space-y-4">
          <div>
            <label className="mb-2 block text-sm font-medium">Title</label>
            <input
              className="h-10 w-full rounded-md border border-border bg-background px-3 text-sm text-foreground outline-none"
              value={newNoteTitle}
              onChange={(e) => setNewNoteTitle(e.target.value)}
              placeholder="Note title"
              onKeyDown={(e) => {
                if (e.key === "Enter") {
                  void createNote();
                }
              }}
              autoFocus
            />
          </div>

          <BucketSelector
            buckets={buckets}
            value={newNoteBucketId}
            onChange={setNewNoteBucketId}
          />

          <div className="flex justify-end gap-2">
            <Button
              variant="secondary"
              onClick={() => {
                setIsModalOpen(false);
                setNewNoteTitle("");
                setNewNoteBucketId(null);
                setError(null);
              }}
            >
              Cancel
            </Button>
            <Button
              onClick={() => void createNote()}
              disabled={isSubmitting || !newNoteTitle.trim()}
            >
              {isSubmitting ? "Creating..." : "Create"}
            </Button>
          </div>
        </div>
      </Modal>
    </section>
  );
}
