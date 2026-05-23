import { Download, Loader2 } from "lucide-react";
import type { ReactNode } from "react";
import type {
  HealthStatus,
  RuntimeStatus,
  SyncStatus,
} from "@entities/runtime";
import { runtimeStateStyles } from "@shared/constants/ui";
import { Button, StatusBadge } from "@shared/ui";

export function RuntimePanel({
  health,
  isSettingUpAi = false,
  onSetupAi,
  runtime,
  sync,
}: {
  health: HealthStatus | null;
  isSettingUpAi?: boolean;
  onSetupAi?: () => void;
  runtime: RuntimeStatus | null;
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
          runtime?.setupRequired && onSetupAi ? (
            <Button
              className="mt-3 w-full"
              variant="secondary"
              onClick={onSetupAi}
              disabled={isSettingUpAi}
            >
              {isSettingUpAi ? (
                <Loader2 className="animate-spin" size={16} aria-hidden />
              ) : (
                <Download size={16} aria-hidden />
              )}
              {isSettingUpAi ? "Installing..." : "Install local AI runtime"}
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
    return "Ready";
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
