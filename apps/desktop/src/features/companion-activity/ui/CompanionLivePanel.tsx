import { Link } from "react-router-dom";

import { cn } from "@shared/lib/cn";

import { useCompanionActivity } from "../model/useCompanionActivity";
import { activityDotClass, formatActivityTime } from "../model/activityFormat";

const PREVIEW_COUNT = 3;

/**
 * A compact, always-on "what's happening right now" strip for the companion home
 * screen — turning it from a menu of pages into a live control panel. Reuses the
 * polling activity hook and shows the latest events with a live indicator.
 */
export function CompanionLivePanel() {
  const { events, isLoading, error } = useCompanionActivity();

  // Stay out of the way until we actually have something to show.
  if (isLoading || error) {
    return null;
  }

  const recent = events.slice(0, PREVIEW_COUNT);

  return (
    <section className="flex flex-col gap-3">
      <div className="flex items-center justify-between px-1">
        <h2 className="flex items-center gap-2 text-xs font-semibold uppercase tracking-wide text-muted-foreground">
          <span className="relative flex h-2 w-2">
            <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-primary/60" />
            <span className="relative inline-flex h-2 w-2 rounded-full bg-primary" />
          </span>
          Right now
        </h2>
        <Link
          to="/companion/activity"
          className="text-xs text-muted-foreground hover:text-foreground"
        >
          View all
        </Link>
      </div>

      {recent.length === 0 ? (
        <p className="px-1 text-sm text-muted-foreground">
          All caught up — nothing processing right now.
        </p>
      ) : (
        <ul className="flex flex-col gap-2 rounded-xl border border-border bg-card p-3">
          {recent.map((event) => (
            <li key={event.id} className="flex items-center gap-2">
              <span
                className={cn(
                  "h-2 w-2 shrink-0 rounded-full",
                  activityDotClass(event.kind),
                )}
              />
              <span className="min-w-0 flex-1 truncate text-sm text-foreground">
                {event.message}
              </span>
              <time className="shrink-0 text-xs text-muted-foreground">
                {formatActivityTime(event.timestamp)}
              </time>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
