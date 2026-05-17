import type { SelectHTMLAttributes } from "react";
import { cn } from "@shared/lib/cn";

type SelectProps = SelectHTMLAttributes<HTMLSelectElement>;

export function Select({ className = "", ...props }: SelectProps) {
  return (
    <select
      className={cn(
        "h-11 w-full rounded-md border border-border bg-card px-4 text-sm leading-5 text-foreground shadow-sm outline-none transition-[border-color,box-shadow,background-color] focus:border-primary focus:ring-2 focus:ring-primary/20 disabled:cursor-not-allowed disabled:bg-muted disabled:opacity-60 [&>option]:bg-card [&>option]:text-foreground",
        className,
      )}
      {...props}
    />
  );
}
