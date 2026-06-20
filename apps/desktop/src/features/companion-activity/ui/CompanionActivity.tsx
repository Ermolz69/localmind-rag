import { cn } from "@shared/lib/cn";

import {
  useCompanionActivity,
  type CompanionActivityEvent,
} from "../model/useCompanionActivity";
import { activityDotClass, formatActivityTime } from "../model/activityFormat";

function ActivityRow({ event }: { event: CompanionActivityEvent }) {
  return (
    <li className="flex gap-3">
      <span className="flex flex-col items-center pt-1.5">
        <span
          className={cn(
            "h-2 w-2 shrink-0 rounded-full",
            activityDotClass(event.kind),
          )}
        />
      </span>
      <div className="min-w-0 flex-1 border-b border-border pb-3">
        <div className="flex items-baseline justify-between gap-2">
          <p className="min-w-0 text-sm text-foreground">{event.message}</p>
          <time className="shrink-0 text-xs text-muted-foreground">
            {formatActivityTime(event.timestamp)}
          </time>
        </div>
        {event.detail ? (
          <p className="mt-0.5 text-xs text-muted-foreground">
            Reason: {event.detail}
          </p>
        ) : null}
      </div>
    </li>
  );
}

export function CompanionActivity() {
  const { events, isLoading, error } = useCompanionActivity();

  if (isLoading) {
    return <p className="text-sm text-muted-foreground">Loading activity…</p>;
  }

  if (error) {
    return <p className="text-destructive text-sm">{error}</p>;
  }

  if (events.length === 0) {
    return (
      <p className="text-sm text-muted-foreground">
        Nothing yet. Activity appears here as LocalMind works on the computer.
      </p>
    );
  }

  return (
    <ul className="flex min-h-0 flex-1 flex-col gap-3 overflow-y-auto">
      {events.map((event) => (
        <ActivityRow key={event.id} event={event} />
      ))}
    </ul>
  );
}
