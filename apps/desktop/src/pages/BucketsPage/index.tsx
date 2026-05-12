import { CheckCircle2, FolderPlus, RefreshCw } from "lucide-react";
import { useCallback, useEffect, useState } from "react";
import { localApi, type BucketDto } from "../../shared/api/client";
import { cn } from "../../shared/lib/cn";
import { Button } from "../../shared/ui/Button";
import { EmptyState } from "../../shared/ui/EmptyState";

export function BucketsPage() {
  const [buckets, setBuckets] = useState<BucketDto[]>([]);
  const [selectedBucketId, setSelectedBucketId] = useState<string>("");
  const [name, setName] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadBuckets = useCallback(async () => {
    setError(null);
    try {
      setBuckets(await localApi.getBuckets());
    } catch (exception) {
      setError(
        exception instanceof Error
          ? exception.message
          : "Unable to load buckets.",
      );
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadBuckets();
  }, [loadBuckets]);

  async function createBucket() {
    const nextName = name.trim();
    if (!nextName) {
      return;
    }

    setError(null);
    try {
      const bucket = await localApi.createBucket(nextName);
      setName("");
      setSelectedBucketId(bucket.id);
      await loadBuckets();
    } catch (exception) {
      setError(
        exception instanceof Error
          ? exception.message
          : "Bucket creation failed.",
      );
    }
  }

  return (
    <section className="space-y-5">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold">Buckets</h1>
          <p className="text-sm text-muted-foreground">
            Organize documents and notes into local workspaces.
          </p>
        </div>
        <Button variant="secondary" onClick={() => void loadBuckets()}>
          <RefreshCw size={16} aria-hidden />
          Refresh
        </Button>
      </div>

      <div className="rounded-md border border-border bg-card p-4">
        <div className="flex flex-wrap gap-2">
          <input
            className="h-9 min-w-64 flex-1 rounded-md border border-border bg-background px-3 text-sm text-foreground"
            placeholder="Bucket name"
            value={name}
            onChange={(event) => setName(event.target.value)}
            onKeyDown={(event) => {
              if (event.key === "Enter") {
                void createBucket();
              }
            }}
          />
          <Button onClick={() => void createBucket()}>
            <FolderPlus size={16} aria-hidden />
            New bucket
          </Button>
        </div>
      </div>

      {error ? (
        <div className="rounded-md border border-border bg-card p-3 text-sm text-muted-foreground">
          {error}
        </div>
      ) : null}

      {isLoading ? (
        <div className="rounded-md border border-border bg-card p-6 text-sm text-muted-foreground">
          Loading buckets...
        </div>
      ) : buckets.length === 0 ? (
        <EmptyState
          icon={<FolderPlus size={18} aria-hidden />}
          title="No buckets yet"
          description="Create a bucket to group documents for focused local work."
        />
      ) : (
        <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
          {buckets.map((bucket) => (
            <button
              key={bucket.id}
              className={cn(
                "rounded-md border border-border bg-card p-4 text-left transition hover:bg-muted",
                selectedBucketId === bucket.id &&
                  "bg-primary text-primary-foreground hover:bg-primary",
              )}
              onClick={() => setSelectedBucketId(bucket.id)}
            >
              <div className="flex items-center justify-between gap-3">
                <h2 className="truncate text-sm font-semibold">
                  {bucket.name}
                </h2>
                {selectedBucketId === bucket.id ? (
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
