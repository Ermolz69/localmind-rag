import {
  CheckCircle2,
  FileText,
  FolderPlus,
  Loader2,
  Play,
  RefreshCw,
  Upload,
} from "lucide-react";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import {
  localApi,
  type BucketDto,
  type DocumentSummary,
  type HealthStatus,
  type RuntimeStatus,
  type SyncStatus,
} from "../../shared/api/client";
import {
  documentStatusStyles,
  runtimeStateStyles,
} from "../../shared/constants/ui";
import { cn } from "../../shared/lib/cn";
import { Button } from "../../shared/ui/Button";
import { EmptyState } from "../../shared/ui/EmptyState";
import { StatusBadge } from "../../shared/ui/StatusBadge";

type UploadState = {
  documentId: string;
  ingestionJobId: string;
  fileName: string;
} | null;

export function DocumentsPage() {
  const fileInputRef = useRef<HTMLInputElement | null>(null);
  const [documents, setDocuments] = useState<DocumentSummary[]>([]);
  const [buckets, setBuckets] = useState<BucketDto[]>([]);
  const [selectedBucketId, setSelectedBucketId] = useState<string>("");
  const [health, setHealth] = useState<HealthStatus | null>(null);
  const [runtime, setRuntime] = useState<RuntimeStatus | null>(null);
  const [sync, setSync] = useState<SyncStatus | null>(null);
  const [lastUpload, setLastUpload] = useState<UploadState>(null);
  const [newBucketName, setNewBucketName] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [isUploading, setIsUploading] = useState(false);
  const [isProcessing, setIsProcessing] = useState(false);
  const [isDragging, setIsDragging] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const selectedBucketName = useMemo(() => {
    if (!selectedBucketId) {
      return "All buckets";
    }

    return (
      buckets.find((bucket) => bucket.id === selectedBucketId)?.name ??
      "Selected bucket"
    );
  }, [buckets, selectedBucketId]);

  const loadData = useCallback(async () => {
    setError(null);
    try {
      const [nextDocuments, nextBuckets, nextHealth, nextRuntime, nextSync] =
        await Promise.all([
          localApi.getDocuments(selectedBucketId || undefined),
          localApi.getBuckets(),
          localApi.getHealth(),
          localApi.getRuntimeStatus(),
          localApi.getSyncStatus(),
        ]);
      setDocuments(nextDocuments);
      setBuckets(nextBuckets);
      setHealth(nextHealth);
      setRuntime(nextRuntime);
      setSync(nextSync);
    } catch (exception) {
      setError(
        exception instanceof Error
          ? exception.message
          : "Unable to load local data.",
      );
    } finally {
      setIsLoading(false);
    }
  }, [selectedBucketId]);

  useEffect(() => {
    void loadData();
  }, [loadData]);

  async function uploadFile(file: File) {
    setIsUploading(true);
    setError(null);
    try {
      const upload = await localApi.uploadDocument(
        file,
        selectedBucketId || undefined,
      );
      setLastUpload({ ...upload, fileName: file.name });
      await loadData();
    } catch (exception) {
      setError(
        exception instanceof Error ? exception.message : "Upload failed.",
      );
    } finally {
      setIsUploading(false);
    }
  }

  async function createBucket() {
    const name = newBucketName.trim();
    if (!name) {
      return;
    }

    setError(null);
    try {
      const bucket = await localApi.createBucket(name);
      setNewBucketName("");
      setSelectedBucketId(bucket.id);
      await loadData();
    } catch (exception) {
      setError(
        exception instanceof Error
          ? exception.message
          : "Bucket creation failed.",
      );
    }
  }

  async function processLastUpload() {
    if (!lastUpload) {
      return;
    }

    setIsProcessing(true);
    setError(null);
    try {
      await localApi.processIngestionJob(lastUpload.ingestionJobId);
      setLastUpload(null);
      await loadData();
    } catch (exception) {
      setError(
        exception instanceof Error ? exception.message : "Ingestion failed.",
      );
    } finally {
      setIsProcessing(false);
    }
  }

  return (
    <section className="space-y-5">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold">Documents</h1>
          <p className="text-sm text-muted-foreground">
            Local files, bucket routing, ingestion status, and runtime
            readiness.
          </p>
        </div>
        <div className="flex items-center gap-2">
          <Button variant="secondary" onClick={() => void loadData()}>
            <RefreshCw size={16} aria-hidden />
            Refresh
          </Button>
          <Button
            onClick={() => fileInputRef.current?.click()}
            disabled={isUploading}
          >
            {isUploading ? (
              <Loader2 className="animate-spin" size={16} aria-hidden />
            ) : (
              <Upload size={16} aria-hidden />
            )}
            Upload
          </Button>
        </div>
      </div>

      <RuntimePanel health={health} runtime={runtime} sync={sync} />

      <div className="grid gap-4 lg:grid-cols-[minmax(0,1fr)_20rem]">
        <div className="space-y-4">
          <div className="flex flex-wrap items-center gap-3 rounded-md border border-border bg-card p-3">
            <label className="text-sm font-medium" htmlFor="bucket-filter">
              Bucket
            </label>
            <select
              id="bucket-filter"
              className="h-9 min-w-48 rounded-md border border-border bg-background px-3 text-sm text-foreground"
              value={selectedBucketId}
              onChange={(event) => setSelectedBucketId(event.target.value)}
            >
              <option value="">All buckets</option>
              {buckets.map((bucket) => (
                <option key={bucket.id} value={bucket.id}>
                  {bucket.name}
                </option>
              ))}
            </select>
            <span className="text-sm text-muted-foreground">
              {selectedBucketName}
            </span>
          </div>

          <label
            className={cn(
              "flex min-h-32 cursor-pointer flex-col items-center justify-center rounded-md border border-dashed border-border bg-card px-4 py-6 text-center transition",
              isDragging && "bg-muted",
            )}
            onDragOver={(event) => {
              event.preventDefault();
              setIsDragging(true);
            }}
            onDragLeave={() => setIsDragging(false)}
            onDrop={(event) => {
              event.preventDefault();
              setIsDragging(false);
              const file = event.dataTransfer.files.item(0);
              if (file) {
                void uploadFile(file);
              }
            }}
          >
            <input
              ref={fileInputRef}
              className="hidden"
              type="file"
              onChange={(event) => {
                const file = event.target.files?.item(0);
                event.currentTarget.value = "";
                if (file) {
                  void uploadFile(file);
                }
              }}
            />
            <Upload
              className="mb-3 text-muted-foreground"
              size={24}
              aria-hidden
            />
            <span className="text-sm font-medium">
              Drop a document here or choose a file
            </span>
            <span className="mt-1 text-xs text-muted-foreground">
              TXT, Markdown, HTML, PDF, DOCX and PPTX can be indexed locally.
            </span>
          </label>

          {lastUpload ? (
            <div className="flex flex-wrap items-center justify-between gap-3 rounded-md border border-border bg-card p-3">
              <div>
                <p className="text-sm font-medium">{lastUpload.fileName}</p>
                <p className="text-xs text-muted-foreground">
                  Queued for ingestion
                </p>
              </div>
              <Button
                onClick={() => void processLastUpload()}
                disabled={isProcessing}
              >
                {isProcessing ? (
                  <Loader2 className="animate-spin" size={16} aria-hidden />
                ) : (
                  <Play size={16} aria-hidden />
                )}
                Process now
              </Button>
            </div>
          ) : null}

          {error ? (
            <div className="rounded-md border border-border bg-card p-3 text-sm text-muted-foreground">
              {error}
            </div>
          ) : null}

          <DocumentList documents={documents} isLoading={isLoading} />
        </div>

        <BucketPanel
          buckets={buckets}
          newBucketName={newBucketName}
          selectedBucketId={selectedBucketId}
          onBucketNameChange={setNewBucketName}
          onCreateBucket={() => void createBucket()}
          onSelectBucket={setSelectedBucketId}
        />
      </div>
    </section>
  );
}

function RuntimePanel({
  health,
  runtime,
  sync,
}: {
  health: HealthStatus | null;
  runtime: RuntimeStatus | null;
  sync: SyncStatus | null;
}) {
  const localApiState = health?.status === "OK" ? "ready" : "warning";
  const aiState = runtime?.modelsAvailable ? "ready" : "warning";
  const syncState = sync?.online ? "ready" : "offline";

  return (
    <div className="grid gap-3 md:grid-cols-3">
      <RuntimeTile
        label="LocalApi"
        value={health?.status === "OK" ? "Connected" : "Waiting"}
        badge="Health"
        className={runtimeStateStyles[localApiState]}
      />
      <RuntimeTile
        label="AI runtime"
        value={
          runtime?.modelsAvailable
            ? "Models ready"
            : (runtime?.aiRuntimeStatus ?? "Unknown")
        }
        badge={runtime?.offlineMode ? "Offline" : "Online"}
        className={runtimeStateStyles[aiState]}
      />
      <RuntimeTile
        label="Sync"
        value={sync?.status ?? "Sync disabled"}
        badge={sync?.online ? "Online" : "Offline"}
        className={runtimeStateStyles[syncState]}
      />
    </div>
  );
}

function RuntimeTile({
  label,
  value,
  badge,
  className,
}: {
  label: string;
  value: string;
  badge: string;
  className?: string;
}) {
  return (
    <div className="rounded-md border border-border bg-card p-4">
      <div className="mb-3 flex items-center justify-between gap-3">
        <p className="text-sm font-medium text-card-foreground">{label}</p>
        <StatusBadge label={badge} className={className} />
      </div>
      <p className="text-sm text-muted-foreground">{value}</p>
    </div>
  );
}

function DocumentList({
  documents,
  isLoading,
}: {
  documents: DocumentSummary[];
  isLoading: boolean;
}) {
  if (isLoading) {
    return (
      <div className="rounded-md border border-border bg-card p-6 text-sm text-muted-foreground">
        Loading documents...
      </div>
    );
  }

  if (documents.length === 0) {
    return (
      <EmptyState
        icon={<FileText size={18} aria-hidden />}
        title="No documents here yet"
        description="Upload a local file to create a queued ingestion job."
      />
    );
  }

  return (
    <div className="overflow-hidden rounded-md border border-border bg-card">
      <div className="grid grid-cols-[minmax(0,1fr)_8rem_10rem] border-b border-border px-4 py-3 text-xs font-medium uppercase text-muted-foreground">
        <span>Name</span>
        <span>Status</span>
        <span>Created</span>
      </div>
      {documents.map((document) => (
        <div
          key={document.id}
          className="grid grid-cols-[minmax(0,1fr)_8rem_10rem] items-center gap-3 border-b border-border px-4 py-3 last:border-b-0"
        >
          <div className="min-w-0">
            <p className="truncate text-sm font-medium text-card-foreground">
              {document.name}
            </p>
            <p className="truncate text-xs text-muted-foreground">
              {document.lastError ?? document.id}
            </p>
          </div>
          <StatusBadge
            label={document.status}
            className={
              documentStatusStyles[document.status] ??
              documentStatusStyles.Queued
            }
          />
          <span className="text-sm text-muted-foreground">
            {new Date(document.createdAt).toLocaleDateString()}
          </span>
        </div>
      ))}
    </div>
  );
}

function BucketPanel({
  buckets,
  selectedBucketId,
  newBucketName,
  onBucketNameChange,
  onCreateBucket,
  onSelectBucket,
}: {
  buckets: BucketDto[];
  selectedBucketId: string;
  newBucketName: string;
  onBucketNameChange: (value: string) => void;
  onCreateBucket: () => void;
  onSelectBucket: (value: string) => void;
}) {
  return (
    <aside className="space-y-4">
      <div className="rounded-md border border-border bg-card p-4">
        <div className="mb-3 flex items-center gap-2">
          <FolderPlus size={17} aria-hidden />
          <h2 className="text-sm font-semibold">Buckets</h2>
        </div>
        <div className="flex gap-2">
          <input
            className="h-9 min-w-0 flex-1 rounded-md border border-border bg-background px-3 text-sm text-foreground"
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
        <button
          className={cn(
            "flex w-full items-center justify-between rounded-md px-3 py-2 text-left text-sm",
            !selectedBucketId
              ? "bg-primary text-primary-foreground"
              : "text-muted-foreground hover:bg-muted hover:text-foreground",
          )}
          onClick={() => onSelectBucket("")}
        >
          <span>All buckets</span>
          {!selectedBucketId ? <CheckCircle2 size={16} aria-hidden /> : null}
        </button>
        {buckets.map((bucket) => (
          <button
            key={bucket.id}
            className={cn(
              "mt-1 flex w-full items-center justify-between rounded-md px-3 py-2 text-left text-sm",
              selectedBucketId === bucket.id
                ? "bg-primary text-primary-foreground"
                : "text-muted-foreground hover:bg-muted hover:text-foreground",
            )}
            onClick={() => onSelectBucket(bucket.id)}
          >
            <span className="truncate">{bucket.name}</span>
            {selectedBucketId === bucket.id ? (
              <CheckCircle2 size={16} aria-hidden />
            ) : null}
          </button>
        ))}
      </div>
    </aside>
  );
}
