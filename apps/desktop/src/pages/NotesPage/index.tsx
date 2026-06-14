import { FileText } from "lucide-react";
import { BucketSelector, NoteEditor } from "@features/note-editor";
import { VaultExplorer } from "@features/vault-explorer";
import {
  Button,
  ConfirmDialog,
  ErrorBanner,
  Input,
  Modal,
  PageHeader,
  Textarea,
} from "@shared/ui";
import { useNotesPageViewModel } from "./model/useNotesPageViewModel";

export function NotesPage() {
  const page = useNotesPageViewModel();

  return (
    <section className="flex min-h-[calc(100dvh-5.5rem)] flex-col space-y-4">
      <PageHeader
        title="Notes vault"
        description="A local markdown workspace with folders, files, and a focused editor."
      />

      <ErrorBanner message={page.error} />

      <div className="grid min-h-0 flex-1 gap-4 xl:grid-cols-[20rem_minmax(0,1fr)]">
        <VaultExplorer
          explorer={page.explorer}
          onCreateFile={() => {
            page.setCreateDraft({
              ...page.createDraft,
              bucketId: page.explorer.selectedBucketId,
              folderId: page.explorer.lastSelectedFolderId,
            });
            page.setIsCreateOpen(true);
          }}
        />

        {page.explorer.selectedNoteId ? (
          <NoteEditor
            buckets={page.explorer.buckets}
            draft={page.draft}
            isDirty={page.isDirty}
            isSubmitting={page.isSubmitting}
            onDraftChange={page.setDraft}
            onSave={() => void page.saveNote()}
            onCancel={page.cancelEdit}
            onDelete={() =>
              page.setDeleteTargetId(page.explorer.selectedNoteId)
            }
          />
        ) : (
          <div className="flex items-center justify-center rounded-md border border-neutral-200 bg-neutral-50 dark:border-neutral-800 dark:bg-neutral-900/50">
            <div className="flex flex-col items-center gap-2 text-neutral-500">
              <FileText size={32} />
              <p>Select a markdown file to edit</p>
            </div>
          </div>
        )}
      </div>

      <Modal
        title="Create markdown file"
        description="Add a local markdown file to your vault."
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
              placeholder="File name"
              autoFocus
            />
          </div>
          <BucketSelector
            buckets={page.explorer.buckets}
            value={page.createDraft.bucketId ?? ""}
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
              placeholder="Start writing..."
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
              {page.isSubmitting ? "Creating..." : "Create file"}
            </Button>
          </div>
        </div>
      </Modal>

      <ConfirmDialog
        open={Boolean(page.deleteTargetId)}
        title="Delete file"
        description="This hides the markdown file from your local vault."
        confirmLabel="Delete"
        isPending={page.isSubmitting}
        onConfirm={() => void page.deleteNote()}
        onClose={() => page.setDeleteTargetId(null)}
      />
    </section>
  );
}
