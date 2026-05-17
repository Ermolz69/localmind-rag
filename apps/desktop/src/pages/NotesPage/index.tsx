import { FileText, Plus } from "lucide-react";
import { BucketSelector, NoteEditor, NoteList } from "@features/note-editor";
import {
  Button,
  ConfirmDialog,
  EmptyState,
  ErrorBanner,
  Input,
  Modal,
  PageHeader,
  Select,
  Textarea,
  Toolbar,
} from "@shared/ui";
import { useNotesPageViewModel } from "./model/useNotesPageViewModel";

export function NotesPage() {
  const page = useNotesPageViewModel();

  return (
    <section className="space-y-4">
      <PageHeader
        title="Notes"
        description="Local markdown notes and document references."
        actions={
          <Button onClick={() => page.setIsCreateOpen(true)}>
            <Plus size={16} aria-hidden />
            New note
          </Button>
        }
      />

      <ErrorBanner message={page.error} />

      <Toolbar>
        <Input
          className="max-w-sm"
          placeholder="Search notes"
          value={page.query}
          onChange={(event) => page.setQuery(event.target.value)}
        />
        <Select
          className="max-w-56"
          value={page.selectedBucketId}
          onChange={(event) => page.setSelectedBucketId(event.target.value)}
        >
          <option value="">All buckets</option>
          {page.buckets.map((bucket) => (
            <option key={bucket.id} value={bucket.id}>
              {bucket.name}
            </option>
          ))}
        </Select>
      </Toolbar>

      <div className="grid gap-4 lg:grid-cols-[minmax(0,300px)_1fr]">
        <NoteList
          notes={page.notes}
          buckets={page.buckets}
          selectedNoteId={page.selectedNoteId}
          isLoading={page.isLoading}
          hasMore={page.hasMore}
          isLoadingMore={page.isLoadingMore}
          onSelect={page.selectNote}
          onLoadMore={() => void page.loadMore()}
        />

        {page.selectedNote ? (
          <NoteEditor
            buckets={page.buckets}
            draft={page.draft}
            isDirty={page.isDirty}
            isSubmitting={page.isSubmitting}
            onDraftChange={page.setDraft}
            onSave={() => void page.saveNote()}
            onCancel={page.cancelEdit}
            onDelete={() =>
              page.setDeleteTargetId(page.selectedNote?.id ?? null)
            }
          />
        ) : (
          <EmptyState
            icon={<FileText size={20} aria-hidden />}
            title="No note selected"
            description="Select a note from the list or create a new one to get started."
          />
        )}
      </div>

      <Modal
        title="Create note"
        description="Create a new markdown note."
        open={page.isCreateOpen}
        onClose={() => page.setIsCreateOpen(false)}
      >
        <div className="space-y-4">
          <div>
            <label className="mb-2 block text-sm font-medium">Title</label>
            <Input
              value={page.createDraft.title}
              onChange={(event) =>
                page.setCreateDraft({
                  ...page.createDraft,
                  title: event.target.value,
                })
              }
              placeholder="Note title"
              autoFocus
            />
          </div>
          <BucketSelector
            buckets={page.buckets}
            value={page.createDraft.bucketId}
            onChange={(bucketId) =>
              page.setCreateDraft({ ...page.createDraft, bucketId })
            }
          />
          <div>
            <label className="mb-2 block text-sm font-medium">Markdown</label>
            <Textarea
              className="min-h-40 font-mono"
              value={page.createDraft.markdown}
              onChange={(event) =>
                page.setCreateDraft({
                  ...page.createDraft,
                  markdown: event.target.value,
                })
              }
              placeholder="Write markdown here..."
            />
          </div>
          <div className="flex justify-end gap-2">
            <Button
              variant="secondary"
              onClick={() => page.setIsCreateOpen(false)}
            >
              Cancel
            </Button>
            <Button
              onClick={() => void page.createNote()}
              disabled={page.isSubmitting || !page.createDraft.title.trim()}
            >
              {page.isSubmitting ? "Creating..." : "Create"}
            </Button>
          </div>
        </div>
      </Modal>

      <ConfirmDialog
        open={Boolean(page.deleteTargetId)}
        title="Delete note"
        description="This hides the note from your local notes list."
        confirmLabel="Delete"
        isPending={page.isSubmitting}
        onConfirm={() => void page.deleteNote()}
        onClose={() => page.setDeleteTargetId(null)}
      />
    </section>
  );
}
