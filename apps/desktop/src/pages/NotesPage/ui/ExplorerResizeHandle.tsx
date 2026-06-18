import React from "react";

interface ExplorerResizeHandleProps {
  onMouseDown: (e: React.MouseEvent) => void;
  onDoubleClick: () => void;
}

export function ExplorerResizeHandle({
  onMouseDown,
  onDoubleClick,
}: ExplorerResizeHandleProps) {
  return (
    <div
      className="group flex w-[6px] cursor-col-resize justify-center transition-colors"
      onMouseDown={onMouseDown}
      onDoubleClick={onDoubleClick}
      role="separator"
      aria-orientation="vertical"
    >
      <div className="h-full w-[1px] bg-neutral-200 transition-colors group-hover:bg-primary/50 group-active:bg-primary/80 dark:bg-neutral-800 dark:group-hover:bg-primary/50 dark:group-active:bg-primary/80" />
    </div>
  );
}
