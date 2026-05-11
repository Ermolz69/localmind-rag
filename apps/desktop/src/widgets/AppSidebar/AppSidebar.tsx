import { BookOpen, Bot, FileText, Folders, Home, Settings } from "lucide-react";
import { NavLink } from "react-router-dom";

const items = [
  { to: "/", label: "Dashboard", icon: Home },
  { to: "/buckets", label: "Buckets", icon: Folders },
  { to: "/documents", label: "Documents", icon: FileText },
  { to: "/notes", label: "Notes", icon: BookOpen },
  { to: "/chat", label: "Chat", icon: Bot },
  { to: "/settings", label: "Settings", icon: Settings },
];

export function AppSidebar() {
  return (
    <aside className="flex h-screen w-64 shrink-0 flex-col border-r border-border bg-card p-3 text-card-foreground">
      <div className="px-2 py-3 text-lg font-semibold">localmind</div>
      <nav className="flex flex-col gap-1">
        {items.map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            className={({ isActive }) =>
              `flex h-10 items-center gap-3 rounded-md px-3 text-sm ${isActive ? "bg-primary text-primary-foreground" : "text-muted-foreground hover:bg-muted hover:text-foreground"}`
            }
          >
            <item.icon size={18} aria-hidden />
            {item.label}
          </NavLink>
        ))}
      </nav>
    </aside>
  );
}
