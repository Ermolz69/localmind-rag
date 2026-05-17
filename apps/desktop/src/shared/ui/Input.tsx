import type { InputHTMLAttributes } from "react";
import { cn } from "@shared/lib/cn";

type InputProps = InputHTMLAttributes<HTMLInputElement>;

export function Input({ className = "", ...props }: InputProps) {
  return (
    <input
      className={cn(
        "h-11 w-full rounded-md border border-border bg-card px-4 text-sm leading-5 text-foreground shadow-sm outline-none transition-[border-color,box-shadow,background-color] placeholder:text-muted-foreground focus:border-primary focus:ring-2 focus:ring-primary/20 disabled:cursor-not-allowed disabled:bg-muted disabled:opacity-60",
        className,
      )}
      {...props}
    />
  );
}
