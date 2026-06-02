import { Button } from "@shared/ui";

type QueuedIngestionNoticeProps = {
  fileName: string;
  isProcessing: boolean;
  onProcess?: () => void;
};

export function QueuedIngestionNotice({
  fileName,
  isProcessing,
  onProcess,
}: QueuedIngestionNoticeProps) {
  return (
    <div className="flex flex-wrap items-center justify-between gap-3 rounded-md border border-border bg-card p-3">
      <div>
        <p className="text-sm font-medium">{fileName}</p>
        <p className="text-xs text-muted-foreground">
          {onProcess
            ? "Queued for ingestion"
            : "Queued for automatic ingestion"}
        </p>
      </div>
      {onProcess ? (
        <Button onClick={onProcess} disabled={isProcessing}>
          {isProcessing ? "Processing..." : "Process now"}
        </Button>
      ) : null}
    </div>
  );
}
