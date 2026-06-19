import { Moon, Sun } from "lucide-react";
import { Button } from "@shared/ui";
import { useTheme } from "@shared/theme/theme-provider";

export function TopBar() {
  const { resolvedTheme, toggleTheme } = useTheme();

  return (
    <header className="flex h-14 items-center justify-between border-b border-border bg-background px-5">
      <div>
        <p className="text-sm font-medium">Offline-first knowledge workspace</p>
        <p className="text-xs text-muted-foreground">
          Local RAG, documents, notes, sync-ready backend
        </p>
      </div>
      <Button className="w-9 px-0" onClick={toggleTheme} title="Toggle theme">
        {resolvedTheme === "dark" ? <Sun size={17} /> : <Moon size={17} />}
      </Button>
    </header>
  );
}
