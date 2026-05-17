import {
  BookOpen,
  Bot,
  ChevronLeft,
  ChevronRight,
  FileText,
  Folders,
  Home,
  Settings,
} from "lucide-react";
import { useEffect, useState } from "react";
import { NavLink } from "react-router-dom";
import { cn } from "@shared/lib/cn";

const items = [
  { to: "/", label: "Dashboard", icon: Home },
  { to: "/buckets", label: "Buckets", icon: Folders },
  { to: "/documents", label: "Documents", icon: FileText },
  { to: "/notes", label: "Notes", icon: BookOpen },
  { to: "/chat", label: "Chat", icon: Bot },
  { to: "/settings", label: "Settings", icon: Settings },
];

const sidebarStorageKey = "localmind.sidebar.expanded";

export function AppSidebar() {
  const [isExpanded, setIsExpanded] = useState(false);

  useEffect(() => {
    setIsExpanded(window.localStorage.getItem(sidebarStorageKey) === "true");
  }, []);

  function setExpanded(nextValue: boolean) {
    setIsExpanded(nextValue);
    window.localStorage.setItem(sidebarStorageKey, String(nextValue));
  }

  return (
    <aside
      className={cn(
        "group/sidebar sticky top-0 flex h-dvh min-h-dvh shrink-0 flex-col overflow-visible border-r border-border bg-card text-card-foreground shadow-sm transition-[width] duration-300 ease-out",
        isExpanded ? "w-64" : "w-16",
      )}
      onClick={() => {
        if (!isExpanded) {
          setExpanded(true);
        }
      }}
    >
      <button
        type="button"
        className={cn(
          "absolute -right-3 top-1/2 z-20 flex h-7 w-7 -translate-y-1/2 items-center justify-center rounded-full border border-border bg-card text-muted-foreground opacity-0 shadow-lg transition-[opacity,transform,color,background-color] duration-200 hover:bg-muted hover:text-foreground focus-visible:opacity-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary/30 group-hover/sidebar:translate-x-1 group-hover/sidebar:opacity-100",
          isExpanded && "opacity-100",
        )}
        aria-label={isExpanded ? "Collapse sidebar" : "Expand sidebar"}
        aria-expanded={isExpanded}
        onClick={(event) => {
          event.stopPropagation();
          setExpanded(!isExpanded);
        }}
      >
        {isExpanded ? (
          <ChevronLeft size={16} aria-hidden />
        ) : (
          <ChevronRight size={16} aria-hidden />
        )}
      </button>

      <div className="flex h-full min-w-0 flex-col overflow-hidden p-3">
        <div
          className={cn(
            "mb-3 flex h-11 items-center rounded-md px-2 transition-[justify-content]",
            isExpanded ? "justify-start" : "justify-center",
          )}
        >
          <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-md bg-primary text-sm font-semibold text-primary-foreground">
            L
          </div>
          <div
            className={cn(
              "ml-3 min-w-0 transition-[opacity,transform] duration-200",
              isExpanded
                ? "translate-x-0 opacity-100"
                : "pointer-events-none -translate-x-2 opacity-0",
            )}
          >
            <p className="truncate text-base font-semibold leading-5">
              localmind
            </p>
            <p className="truncate text-xs leading-4 text-muted-foreground">
              Local RAG
            </p>
          </div>
        </div>

        <nav className="flex flex-col gap-1">
          {items.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              title={isExpanded ? undefined : item.label}
              className={({ isActive }) =>
                cn(
                  "flex h-11 min-w-0 items-center rounded-md text-sm transition-[background-color,color,padding] duration-200",
                  isExpanded ? "gap-3 px-3" : "justify-center px-0",
                  isActive
                    ? "bg-primary text-primary-foreground"
                    : "text-muted-foreground hover:bg-muted hover:text-foreground",
                )
              }
            >
              <item.icon className="shrink-0" size={18} aria-hidden />
              <span
                className={cn(
                  "min-w-0 truncate transition-[opacity,transform] duration-200",
                  isExpanded
                    ? "translate-x-0 opacity-100"
                    : "pointer-events-none w-0 -translate-x-2 opacity-0",
                )}
              >
                {item.label}
              </span>
            </NavLink>
          ))}
        </nav>
      </div>
    </aside>
  );
}
