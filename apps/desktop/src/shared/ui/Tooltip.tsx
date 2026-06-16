import type { ReactNode } from "react";
import { cn } from "@shared/lib/cn";

type TooltipProps = {
  content: ReactNode;
  children: ReactNode;
  className?: string;
};

export function Tooltip({ content, children, className = "" }: TooltipProps) {
  if (!content) return <>{children}</>;

  return (
    <div className={cn("group relative inline-flex", className)}>
      {children}
      <div className="pointer-events-none absolute bottom-full left-1/2 z-50 mb-2 -translate-x-1/2 translate-y-1 opacity-0 transition-all duration-200 group-hover:translate-y-0 group-hover:opacity-100">
        <div className="whitespace-nowrap rounded-md bg-foreground px-2.5 py-1 text-xs text-background shadow-md">
          {content}
        </div>
        <div className="absolute left-1/2 top-full -mt-0.5 h-2 w-2 -translate-x-1/2 rotate-45 bg-foreground" />
      </div>
    </div>
  );
}
