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
      <div className="h-full w-[1px] bg-border transition-colors group-hover:bg-primary/50 group-active:bg-primary/80" />
    </div>
  );
}
