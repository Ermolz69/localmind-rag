import { X } from "lucide-react";
import type { PropsWithChildren } from "react";
import { createPortal } from "react-dom";
import { cn } from "../lib/cn";
import { Button } from "./Button";

type ModalProps = PropsWithChildren<{
  title: string;
  description?: string;
  open: boolean;
  onClose: () => void;
  panelClassName?: string;
  bodyClassName?: string;
}>;

export function Modal({
  title,
  description,
  open,
  onClose,
  panelClassName,
  bodyClassName,
  children,
}: ModalProps) {
  if (!open) {
    return null;
  }

  return createPortal(
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-background/80 px-4 backdrop-blur-sm">
      <div
        className={cn(
          "w-full max-w-lg rounded-md border border-border bg-card p-5 text-card-foreground shadow-xl",
          panelClassName,
        )}
      >
        <div className="flex items-start justify-between gap-4">
          <div>
            <h2 className="text-lg font-semibold">{title}</h2>
            {description ? (
              <p className="mt-1 text-sm text-muted-foreground">
                {description}
              </p>
            ) : null}
          </div>
          <Button
            className="!h-9 !w-9 bg-muted px-0 text-muted-foreground"
            onClick={onClose}
          >
            <X size={18} aria-hidden />
          </Button>
        </div>
        <div className={cn("mt-5", bodyClassName)}>{children}</div>
      </div>
    </div>,
    document.body,
  );
}
