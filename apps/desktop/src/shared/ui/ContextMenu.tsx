import { useEffect, useRef } from "react";
import { createPortal } from "react-dom";

export function ContextMenu({
  x,
  y,
  onClose,
  children,
}: {
  x: number;
  y: number;
  onClose: () => void;
  children: React.ReactNode;
}) {
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    function onDown(e: MouseEvent) {
      if (!ref.current?.contains(e.target as Node)) onClose();
    }
    function onKey(e: KeyboardEvent) {
      if (e.key === "Escape") onClose();
    }
    document.addEventListener("mousedown", onDown);
    document.addEventListener("keydown", onKey);
    return () => {
      document.removeEventListener("mousedown", onDown);
      document.removeEventListener("keydown", onKey);
    };
  }, [onClose]);

  const menuWidth = 176;
  const left = x + menuWidth > window.innerWidth ? x - menuWidth : x;

  return createPortal(
    <div
      ref={ref}
      style={{ top: y, left }}
      className="fixed z-50 min-w-[11rem] overflow-hidden rounded-md border border-border bg-card py-1 shadow-lg"
      role="menu"
      onContextMenu={(e) => e.preventDefault()}
    >
      {children}
    </div>,
    document.body,
  );
}

export function ContextMenuItem({
  icon,
  label,
  onClick,
  variant = "default",
  disabled = false,
}: {
  icon?: React.ReactNode;
  label: string;
  onClick: () => void;
  variant?: "default" | "destructive";
  disabled?: boolean;
}) {
  return (
    <button
      role="menuitem"
      disabled={disabled}
      className={`flex w-full items-center gap-2 px-3 py-1.5 text-left text-sm transition-colors hover:bg-muted disabled:pointer-events-none disabled:opacity-50 ${
        variant === "destructive" ? "text-destructive" : "text-card-foreground"
      }`}
      onClick={onClick}
    >
      {icon}
      {label}
    </button>
  );
}

export function ContextMenuSeparator() {
  return <div role="separator" className="my-1 border-t border-border" />;
}
