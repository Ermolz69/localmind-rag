import { FolderPlus, RefreshCw, Pencil, Trash2, Folder } from "lucide-react";
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
            No buckets found matching "{bucketsPage.name}". Click "New bucket"
            to create it!
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
                  "group relative cursor-pointer overflow-hidden rounded-xl border border-border/50 bg-card p-5 text-left transition-all duration-300 hover:-translate-y-0.5 hover:border-border hover:shadow-md",
                  isSelected
                    ? "bg-primary/5 ring-2 ring-primary"
                    : "hover:bg-accent/30",
                )}
                onClick={() => bucketsPage.setSelectedBucketId(bucket.id)}
              >
                <div className="flex items-start justify-between gap-4">
                  <div className="flex min-w-0 flex-1 items-start gap-3">
                    <div
                      className={cn(
                        "mt-0.5 flex h-10 w-10 shrink-0 items-center justify-center rounded-lg border transition-colors",
                        isSelected
                          ? "border-primary bg-primary text-primary-foreground"
                          : "border-border/50 bg-muted/50 text-muted-foreground group-hover:bg-background",
                      )}
                    >
                      <Folder size={18} />
                    </div>
                    <div className="min-w-0 flex-1">
                      <h2
                        className="truncate text-base font-semibold leading-tight text-foreground"
                        title={bucket.name}
                      >
                        {bucket.name}
                      </h2>
                      <div className="mt-1.5 flex items-center gap-2 text-xs text-muted-foreground">
                        {bucket.syncStatus && (
                          <span className="inline-flex items-center gap-1.5">
                            <span className="flex h-1.5 w-1.5 rounded-full bg-emerald-500/80"></span>
                            {bucket.syncStatus}
                          </span>
                        )}
                        {documentCount !== undefined ? (
                          <>
                            {bucket.syncStatus && (
                              <span className="h-3 w-[1px] bg-border/80"></span>
                            )}
                            <span className="truncate">
                              {documentCount} documents
                            </span>
                          </>
                        ) : null}
                      </div>
                    </div>
                  </div>

                  <div
                    className={cn(
                      "flex items-center gap-1.5 rounded-lg border border-transparent bg-background/40 p-1 opacity-0 transition-all focus-within:opacity-100 group-hover:border-border/50 group-hover:opacity-100",
                      isSelected ? "border-border/50 opacity-100" : "",
                    )}
                    onClick={(e) => e.stopPropagation()}
                  >
                    <Button
                      variant="ghost"
                      className="h-9 w-9 rounded-md p-0 text-muted-foreground hover:bg-muted hover:text-foreground"
                      onClick={(e) => {
                        e.stopPropagation();
                        setEditBucketId(bucket.id);
                        setEditBucketName(bucket.name);
                        setRenameModalOpen(true);
                      }}
                    >
                      <Pencil size={18} aria-hidden />
                    </Button>

                    {bucket.name !== "Default" && (
                      <Button
                        variant="ghost"
                        className="h-9 w-9 rounded-md bg-transparent p-0 text-rose-400 hover:bg-rose-500/10 hover:text-rose-500"
                        onClick={(e) => {
                          e.stopPropagation();
                          setDeleteBucketId(bucket.id);
                          setDeleteConfirmOpen(true);
                        }}
                      >
                        <Trash2 size={18} aria-hidden />
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
