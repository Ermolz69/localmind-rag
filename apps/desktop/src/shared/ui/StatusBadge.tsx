import { cn } from "../lib/cn";

type StatusBadgeProps = {
  label: string;
  className?: string;
};

export function StatusBadge({ label, className }: StatusBadgeProps) {
  return (
    <span
      className={cn(
        "inline-flex h-7 items-center rounded-md border px-2 text-xs font-medium",
        className,
      )}
    >
      {label}
    </span>
  );
}
