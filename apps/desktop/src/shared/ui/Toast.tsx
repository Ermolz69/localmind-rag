import { X } from "lucide-react";

import { cn } from "@shared/lib/cn";

export type ToastVariant = "info" | "success" | "error";

type ToastProps = {
  message: string | null;
  variant?: ToastVariant;
  onDismiss?: () => void;
};

const variantClasses: Record<ToastVariant, string> = {
  info: "border-border text-card-foreground",
  success: "border-green-500/40 text-green-500",
  error: "border-destructive text-destructive",
};

export function Toast({ message, variant = "info", onDismiss }: ToastProps) {
  if (!message) {
    return null;
  }

  return (
    <div
      role="status"
      aria-live={variant === "error" ? "assertive" : "polite"}
      className={cn(
        "fixed bottom-4 right-4 z-50 flex max-w-sm items-start gap-3 rounded-md border bg-card px-4 py-3 text-sm shadow-lg",
        variantClasses[variant],
      )}
    >
      <span className="min-w-0 flex-1">{message}</span>
      {onDismiss ? (
        <button
          type="button"
          aria-label="Dismiss notification"
          className="shrink-0 text-muted-foreground hover:text-foreground"
          onClick={onDismiss}
        >
          <X className="h-4 w-4" />
        </button>
      ) : null}
    </div>
  );
}
