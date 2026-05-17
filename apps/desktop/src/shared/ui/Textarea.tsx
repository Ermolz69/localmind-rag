import type { TextareaHTMLAttributes } from "react";
import { cn } from "@shared/lib/cn";

type TextareaProps = TextareaHTMLAttributes<HTMLTextAreaElement>;

export function Textarea({ className = "", ...props }: TextareaProps) {
  return (
    <textarea
      className={cn(
        "min-h-28 w-full rounded-md border border-border bg-card px-4 py-3 text-sm leading-6 text-foreground shadow-sm outline-none transition-[border-color,box-shadow,background-color] placeholder:text-muted-foreground focus:border-primary focus:ring-2 focus:ring-primary/20 disabled:cursor-not-allowed disabled:bg-muted disabled:opacity-60",
        className,
      )}
      {...props}
    />
  );
}
