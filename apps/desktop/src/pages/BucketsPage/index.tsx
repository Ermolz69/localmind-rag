import { FolderPlus, RefreshCw, Pencil, Trash2 } from "lucide-react";
import { useBuckets } from "@features/bucket-management";
import { cn } from "@shared/lib/cn";
import {
  Button,
  EmptyState,
  ErrorBanner,
  Input,
  PageHeader,
  ConfirmDialog,
  Modal,
} from "@shared/ui";
import { useState } from "react";

export function BucketsPage() {
  const bucketsPage = useBuckets();
  const [renameModalOpen, setRenameModalOpen] = useState(false);
  const [editBucketId, setEditBucketId] = useState<string | null>(null);
  const [editBucketName, setEditBucketName] = useState("");
  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false);
  const [deleteBucketId, setDeleteBucketId] = useState("");

  const filteredBuckets = bucketsPage.buckets.filter((bucket) =>
    bucket.name.toLowerCase().includes(bucketsPage.name.toLowerCase().trim()),
  );

  return (
    <section className="space-y-5">
      <PageHeader
        title="Buckets"
        description="Organize documents and notes into local workspaces."
        actions={
          <Button
            variant="secondary"
            onClick={() => void bucketsPage.loadBuckets()}
          >
            <RefreshCw size={16} aria-hidden />
            Refresh
          </Button>
        }
      />

      <div className="rounded-md border border-border bg-card p-4">
        <div className="flex flex-wrap gap-2">
          <Input
            className="min-w-64 flex-1"
            placeholder="Search or create bucket..."
            value={bucketsPage.name}
            onChange={(event) => bucketsPage.setName(event.target.value)}
            onKeyDown={(event) => {
              if (event.key === "Enter") {
                void bucketsPage.createBucket();
              }
            }}
          />
          <Button
            onClick={() => void bucketsPage.createBucket()}
            className="!h-11 px-4"
          >
            <FolderPlus size={16} aria-hidden />
            New bucket
          </Button>
        </div>
      </div>

      <ErrorBanner message={bucketsPage.error} />

      {bucketsPage.isLoading ? (
        <div className="rounded-md border border-border bg-card p-6 text-sm text-muted-foreground">
          Loading buckets...
        </div>
      ) : bucketsPage.buckets.length === 0 ? (
        <EmptyState
          icon={<FolderPlus size={18} aria-hidden />}
          title="No buckets yet"
          description="Create a bucket to group documents for focused local work."
        />
      ) : filteredBuckets.length === 0 && bucketsPage.name.trim() !== "" ? (
        <div className="rounded-md border border-border bg-card p-8 text-center">
          <p className="text-sm text-muted-foreground">
            No buckets found matching "{bucketsPage.name}". Click "New bucket" to create it!
          </p>
        </div>
      ) : (
        <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
          {filteredBuckets.map((bucket) => {
            const isSelected = bucketsPage.selectedBucketId === bucket.id;
            const bucketWithCount = bucket as typeof bucket & {
              documentCount?: number;
            };
            const documentCount = bucketWithCount.documentCount;
            return (
              <div
                key={bucket.id}
                className={cn(
                  "cursor-pointer rounded-md border border-border bg-card p-4 text-left transition hover:bg-muted",
                  isSelected &&
                    "bg-primary text-primary-foreground hover:bg-primary",
                )}
                onClick={() => bucketsPage.setSelectedBucketId(bucket.id)}
              >
                <div className="flex items-start justify-between gap-3">
                  <div className="min-w-0 flex-1">
                    <h2 className="truncate text-sm font-semibold" title={bucket.name}>
                      {bucket.name}
                    </h2>
                    <p
                      className={cn(
                        "mt-2 text-xs",
                        isSelected
                          ? "text-primary-foreground/90"
                          : "text-muted-foreground",
                      )}
                    >
                      {bucket.syncStatus}{" "}
                      {documentCount !== undefined ? (
                        <span
                          className={
                            isSelected ? "text-white" : "text-muted-foreground"
                          }
                        >
                          · {documentCount} documents
                        </span>
                      ) : null}
                    </p>
                  </div>
                  <div
                    className={cn(
                      "flex items-center gap-2 rounded-md p-1 transition-colors",
                      isSelected
                        ? "bg-primary/10 ring-1 ring-primary/20"
                        : "hover:bg-muted/50",
                    )}
                    onClick={(e) => e.stopPropagation()}
                  >
                    <Button
                      variant="ghost"
                      className={cn(
                        "h-9 w-9 p-0",
                        "rounded-md",
                        isSelected
                          ? "text-primary-foreground hover:bg-primary/20"
                          : "text-muted-foreground hover:bg-muted",
                      )}
                      onClick={(e) => {
                        e.stopPropagation();
                        setEditBucketId(bucket.id);
                        setEditBucketName(bucket.name);
                        setRenameModalOpen(true);
                      }}
                    >
                      <Pencil size={16} aria-hidden />
                    </Button>

                    {bucket.name !== "Default" && (
                      <Button
                        variant="ghost"
                        className="h-9 w-9 rounded-md bg-transparent p-0 text-rose-400 hover:bg-transparent hover:text-rose-500"
                        onClick={(e) => {
                          e.stopPropagation();
                          setDeleteBucketId(bucket.id);
                          setDeleteConfirmOpen(true);
                        }}
                      >
                        <Trash2 size={16} aria-hidden />
                      </Button>
                    )}
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      )}

      <Modal
        title="Rename bucket"
        description="Update the bucket name."
        open={renameModalOpen}
        onClose={() => setRenameModalOpen(false)}
      >
        <div className="flex gap-2">
          <Input
            value={editBucketName}
            onChange={(e) => setEditBucketName(e.target.value)}
          />
          <Button
            className="!h-11 px-4"
            onClick={async () => {
              if (editBucketId) {
                await bucketsPage.renameBucket(editBucketId, editBucketName);
                setRenameModalOpen(false);
              }
            }}
          >
            Save
          </Button>
        </div>
      </Modal>

      <ConfirmDialog
        open={deleteConfirmOpen}
        title="Delete bucket"
        description="Deleting a bucket will remove its local record. This cannot be undone."
        confirmLabel="Delete"
        onClose={() => setDeleteConfirmOpen(false)}
        onConfirm={async () => {
          if (deleteBucketId) {
            await bucketsPage.deleteBucket(deleteBucketId);
            setDeleteConfirmOpen(false);
            setDeleteBucketId("");
          }
        }}
      />
    </section>
  );
}
