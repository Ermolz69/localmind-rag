import { CheckCircle2, FolderPlus, Search } from "lucide-react";
import type { BucketDto } from "@entities/bucket";
import { Button, Input } from "@shared/ui";
import { cn } from "@shared/lib/cn";

type BucketPanelProps = {
  buckets: BucketDto[];
  bucketQuery: string;
  hasMore: boolean;
  isLoading: boolean;
  isLoadingMore: boolean;
  selectedBucketId: string;
  newBucketName: string;
  onBucketNameChange: (value: string) => void;
  onCreateBucket: () => void;
  onLoadMore: () => void;
  onQueryChange: (value: string) => void;
  onSelectBucket: (value: string) => void;
};

export function BucketPanel({
  buckets,
  bucketQuery,
  hasMore,
  isLoading,
  isLoadingMore,
  selectedBucketId,
  newBucketName,
  onBucketNameChange,
  onCreateBucket,
  onLoadMore,
  onQueryChange,
  onSelectBucket,
}: BucketPanelProps) {
  return (
    <aside className="space-y-4">
      <div className="rounded-md border border-border bg-card p-4">
        <div className="mb-3 flex items-center gap-2">
          <FolderPlus size={17} aria-hidden />
          <h2 className="text-sm font-semibold">Buckets</h2>
        </div>
        <div className="flex gap-2">
          <Input
            placeholder="New bucket"
            value={newBucketName}
            onChange={(event) => onBucketNameChange(event.target.value)}
            onKeyDown={(event) => {
              if (event.key === "Enter") {
                onCreateBucket();
              }
            }}
          />
          <Button className="shrink-0" onClick={onCreateBucket}>
            Create
          </Button>
        </div>
      </div>

      <div className="rounded-xl border border-border bg-card shadow-sm">
        <div className="border-b border-border p-3">
          <div className="relative">
            <Search
              className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground"
              size={16}
              aria-hidden
            />
            <Input
              className="h-10 pl-9"
              placeholder="Search buckets"
              value={bucketQuery}
              onChange={(event) => onQueryChange(event.target.value)}
            />
          </div>
        </div>
        <div className="max-h-[32rem] overflow-auto p-2">
          <BucketButton
            active={!selectedBucketId}
            label="All buckets"
            onClick={() => onSelectBucket("")}
          />
          {isLoading ? (
            <div className="px-3 py-4 text-sm text-muted-foreground">
              Loading buckets...
            </div>
          ) : (
            buckets.map((bucket) => (
              <BucketButton
                key={bucket.id}
                active={selectedBucketId === bucket.id}
                label={bucket.name}
                onClick={() => onSelectBucket(bucket.id)}
              />
            ))
          )}
        </div>
        {hasMore ? (
          <div className="border-t border-border p-2">
            <Button
              className="w-full"
              variant="secondary"
              onClick={onLoadMore}
              disabled={isLoadingMore}
            >
              {isLoadingMore ? "Loading..." : "Load more buckets"}
            </Button>
          </div>
        ) : null}
      </div>
    </aside>
  );
}

function BucketButton({
  active,
  label,
  onClick,
}: {
  active: boolean;
  label: string;
  onClick: () => void;
}) {
  return (
    <button
      className={cn(
        "mt-1 flex w-full items-center justify-between rounded-md px-3 py-2 text-left text-sm first:mt-0",
        active
          ? "bg-primary text-primary-foreground"
          : "text-muted-foreground hover:bg-muted hover:text-foreground",
      )}
      onClick={onClick}
    >
      <span className="truncate">{label}</span>
      {active ? <CheckCircle2 size={16} aria-hidden /> : null}
    </button>
  );
}
