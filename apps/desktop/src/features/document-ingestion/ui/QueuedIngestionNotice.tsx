import { Loader2, X } from "lucide-react";
import type { IngestionJobDto } from "@shared/contracts";
import { ACTIVE_INGESTION_JOB_STATUSES } from "@shared/constants/ui";
import { Button } from "@shared/ui";

type QueuedIngestionNoticeProps = {
  fileName: string;
  isProcessing: boolean;
  job?: IngestionJobDto;
  onProcess?: () => void;
  onCancel?: () => void;
};

export function QueuedIngestionNotice({
  fileName,
  isProcessing,
  job,
  onProcess,
  onCancel,
}: QueuedIngestionNoticeProps) {
  return (
    <div className="flex flex-wrap items-center justify-between gap-3 rounded-md border border-border bg-card p-3">
      <div>
        <p className="text-sm font-medium">{fileName}</p>
        <div className="mt-1 text-xs text-muted-foreground">
          {job && ACTIVE_INGESTION_JOB_STATUSES.has(job.status) ? (
            <span className="flex items-center gap-2">
              <Loader2 className="animate-spin" size={14} aria-hidden />
              <span>{job.currentStep}</span>
              <span className="font-mono">{job.progressPercent}%</span>
            </span>
          ) : onProcess ? (
            "Queued for ingestion"
          ) : (
            "Queued for automatic ingestion"
          )}
        </div>
      </div>
      <div className="flex gap-2">
        {job?.canCancel && onCancel ? (
          <Button variant="secondary" onClick={onCancel}>
            <X size={16} aria-hidden /> Cancel
          </Button>
        ) : null}
        {!job && onProcess ? (
          <Button onClick={onProcess} disabled={isProcessing}>
            {isProcessing ? "Processing..." : "Process now"}
          </Button>
        ) : null}
      </div>
    </div>
  );
}
