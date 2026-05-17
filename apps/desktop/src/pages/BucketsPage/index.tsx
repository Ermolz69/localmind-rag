import { CheckCircle2, FolderPlus, RefreshCw } from "lucide-react";
import { useBuckets } from "@features/bucket-management";
import { cn } from "@shared/lib/cn";
import { Button, EmptyState, ErrorBanner, Input, PageHeader } from "@shared/ui";

export function BucketsPage() {
  const bucketsPage = useBuckets();

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
            placeholder="Bucket name"
            value={bucketsPage.name}
            onChange={(event) => bucketsPage.setName(event.target.value)}
            onKeyDown={(event) => {
              if (event.key === "Enter") {
                void bucketsPage.createBucket();
              }
            }}
          />
          <Button onClick={() => void bucketsPage.createBucket()}>
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
      ) : (
        <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
          {bucketsPage.buckets.map((bucket) => (
            <button
              key={bucket.id}
              className={cn(
                "rounded-md border border-border bg-card p-4 text-left transition hover:bg-muted",
                bucketsPage.selectedBucketId === bucket.id &&
                  "bg-primary text-primary-foreground hover:bg-primary",
              )}
              onClick={() => bucketsPage.setSelectedBucketId(bucket.id)}
            >
              <div className="flex items-center justify-between gap-3">
                <h2 className="truncate text-sm font-semibold">
                  {bucket.name}
                </h2>
                {bucketsPage.selectedBucketId === bucket.id ? (
                  <CheckCircle2 size={17} aria-hidden />
                ) : null}
              </div>
              <p className="mt-2 text-xs text-muted-foreground">
                {bucket.syncStatus}
              </p>
            </button>
          ))}
        </div>
      )}
    </section>
  );
}
