import { Download } from "lucide-react";
import type { ReactNode } from "react";
import type {
  HealthStatus,
  RuntimeStatus,
  SyncStatus,
  RuntimeSetupProgress,
} from "@entities/runtime";
import { runtimeStateStyles } from "@shared/constants/ui";
import { Button, StatusBadge } from "@shared/ui";

export function RuntimePanel({
  health,
  isSettingUpAi = false,
  onSetupAi,
  runtime,
  setupProgress,
  sync,
}: {
  health: HealthStatus | null;
  isSettingUpAi?: boolean;
  onSetupAi?: () => void;
  runtime: RuntimeStatus | null;
  setupProgress?: RuntimeSetupProgress | null;
  sync: SyncStatus | null;
}) {
  const apiState = health?.status === "OK" ? "ready" : "warning";
  const aiState =
    runtime?.aiRuntimeStatus === "Running"
      ? "ready"
      : runtime?.setupRequired
        ? "warning"
        : "offline";
  const syncState = sync?.online ? "ready" : "offline";

  return (
    <div className="grid gap-3 md:grid-cols-3">
      <RuntimeTile
        label="LocalApi"
        value={health?.status === "OK" ? "Connected" : "Waiting"}
        badge="Health"
        className={runtimeStateStyles[apiState]}
      />
      <RuntimeTile
        label="AI runtime"
        value={getAiRuntimeValue(runtime)}
        badge={runtime?.offlineMode ? "Local" : "Online"}
        className={runtimeStateStyles[aiState]}
        detail={runtime?.setupReason ?? undefined}
        action={
          isSettingUpAi ? (
            <div className="mt-3 space-y-2">
              <div className="flex justify-between text-xs text-muted-foreground">
                <span className="truncate pr-2">
                  {setupProgress?.message || "Installing..."}
                </span>
                {setupProgress?.speedBytesPerSecond && (
                  <span className="shrink-0">
                    {(setupProgress.speedBytesPerSecond / 1024 / 1024).toFixed(
                      1,
                    )}{" "}
                    MB/s
                  </span>
                )}
              </div>
              <div className="bg-secondary relative h-2 w-full overflow-hidden rounded-full">
                {setupProgress?.totalBytes && setupProgress?.downloadedBytes ? (
                  <div
                    className="h-full bg-primary transition-all duration-300"
                    style={{
                      width: `${Math.round((setupProgress.downloadedBytes / setupProgress.totalBytes) * 100)}%`,
                    }}
                  />
                ) : (
                  <div className="h-full w-1/3 animate-[pulse_1.5s_ease-in-out_infinite] bg-primary" />
                )}
              </div>
              <div className="text-right text-xs text-muted-foreground">
                {setupProgress?.downloadedBytes && setupProgress?.totalBytes
                  ? `${(setupProgress.downloadedBytes / 1024 / 1024).toFixed(1)} MB / ${(setupProgress.totalBytes / 1024 / 1024).toFixed(1)} MB`
                  : null}
              </div>
            </div>
          ) : runtime?.setupRequired && onSetupAi ? (
            <Button
              className="mt-3 w-full"
              variant="secondary"
              onClick={onSetupAi}
              disabled={isSettingUpAi}
            >
              <Download size={16} aria-hidden />
              Install local AI runtime
            </Button>
          ) : null
        }
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

function getAiRuntimeValue(runtime: RuntimeStatus | null) {
  if (!runtime) {
    return "Unknown";
  }

  if (runtime.aiRuntimeStatus === "Running") {
    return runtime.chatModelName ?? "Ready";
  }

  if (runtime.setupRequired) {
    return "Setup required";
  }

  return runtime.aiRuntimeStatus;
}

function RuntimeTile({
  action,
  detail,
  label,
  value,
  badge,
  className,
}: {
  action?: ReactNode;
  detail?: string;
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
      {detail ? (
        <p className="mt-2 text-xs leading-5 text-muted-foreground">{detail}</p>
      ) : null}
      {action}
    </div>
  );
}
