import type { PropsWithChildren } from "react";
import { cn } from "@shared/lib/cn";

type ToolbarProps = PropsWithChildren<{
  className?: string;
}>;

export function Toolbar({ children, className = "" }: ToolbarProps) {
  return (
    <div
      className={cn(
        "flex flex-wrap items-center gap-3 rounded-md border border-border bg-card p-3",
        className,
      )}
    >
      {children}
    </div>
  );
}
