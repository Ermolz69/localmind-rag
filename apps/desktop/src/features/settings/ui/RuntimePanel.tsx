import type {
  HealthStatus,
  RuntimeStatus,
  SyncStatus,
} from "@entities/runtime";
import { runtimeStateStyles } from "@shared/constants/ui";
import { StatusBadge } from "@shared/ui";

export function RuntimePanel({
  health,
  runtime,
  sync,
}: {
  health: HealthStatus | null;
  runtime: RuntimeStatus | null;
  sync: SyncStatus | null;
}) {
  const apiState = health?.status === "OK" ? "ready" : "warning";
  const aiState = runtime?.modelsAvailable ? "ready" : "warning";
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
