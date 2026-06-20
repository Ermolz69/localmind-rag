import {
  Activity,
  FileText,
  FolderInput,
  MessageSquare,
  Search,
  type LucideIcon,
} from "lucide-react";
import { Link, useLocation } from "react-router-dom";

import { cn } from "@shared/lib/cn";

const TABS: { key: string; label: string; icon: LucideIcon }[] = [
  { key: "chat", label: "Chat", icon: MessageSquare },
  { key: "search", label: "Search", icon: Search },
  { key: "documents", label: "Docs", icon: FileText },
  { key: "files", label: "Files", icon: FolderInput },
  { key: "activity", label: "Activity", icon: Activity },
];

/**
 * Bottom tab bar for quick switching between the main companion screens without
 * detouring through the home screen. Mobile-first: thumb-reachable, active tab
 * highlighted.
 */
export function CompanionTabBar() {
  const { pathname } = useLocation();

  return (
    <nav
      aria-label="Companion sections"
      className="mt-2 border-t border-border pt-2"
    >
      <ul className="flex items-stretch justify-between">
        {TABS.map((tab) => {
          const to = `/companion/${tab.key}`;
          const isActive = pathname === to;
          const Icon = tab.icon;

          return (
            <li key={tab.key} className="flex-1">
              <Link
                to={to}
                aria-current={isActive ? "page" : undefined}
                className={cn(
                  "flex flex-col items-center gap-0.5 rounded-lg py-1.5 text-xs transition",
                  isActive
                    ? "text-primary"
                    : "text-muted-foreground hover:text-foreground",
                )}
              >
                <Icon className="h-5 w-5" />
                {tab.label}
              </Link>
            </li>
          );
        })}
      </ul>
    </nav>
  );
}
