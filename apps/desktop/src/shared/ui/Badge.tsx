import type { PropsWithChildren } from "react";
import { cn } from "@shared/lib/cn";

type BadgeProps = PropsWithChildren<{
  className?: string;
}>;

export function Badge({ children, className = "" }: BadgeProps) {
  return (
    <span
      className={cn(
        "inline-flex h-7 items-center rounded-md border border-border bg-muted px-2 text-xs font-medium text-muted-foreground",
        className,
      )}
    >
      {children}
    </span>
  );
}
