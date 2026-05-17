import { CheckCircle2, FolderPlus } from "lucide-react";
import type { BucketDto } from "@entities/bucket";
import { Button } from "@shared/ui";
import { Input } from "@shared/ui";
import { cn } from "@shared/lib/cn";

type BucketPanelProps = {
  buckets: BucketDto[];
  selectedBucketId: string;
  newBucketName: string;
  onBucketNameChange: (value: string) => void;
  onCreateBucket: () => void;
  onSelectBucket: (value: string) => void;
};

export function BucketPanel({
  buckets,
  selectedBucketId,
  newBucketName,
  onBucketNameChange,
  onCreateBucket,
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

      <div className="rounded-md border border-border bg-card p-2">
        <BucketButton
          active={!selectedBucketId}
          label="All buckets"
          onClick={() => onSelectBucket("")}
        />
        {buckets.map((bucket) => (
          <BucketButton
            key={bucket.id}
            active={selectedBucketId === bucket.id}
            label={bucket.name}
            onClick={() => onSelectBucket(bucket.id)}
          />
        ))}
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
