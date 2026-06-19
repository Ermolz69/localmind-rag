import { FileText, Folder } from "lucide-react";
import { useEffect, useRef, useState } from "react";
import { cn } from "@shared/lib/cn";

export interface InlineExplorerInputProps {
  type: "folder" | "note";
  initialValue?: string;
  paddingLeft?: number;
  isRename?: boolean;
  onSubmit: (value: string) => void;
  onCancel: () => void;
}

export function InlineExplorerInput({
  type,
  initialValue = "",
  paddingLeft = 0,
  isRename = false,
  onSubmit,
  onCancel,
}: InlineExplorerInputProps) {
  const [value, setValue] = useState(initialValue);
  const isCancelled = useRef(false);

  const handleSubmit = () => {
    if (isCancelled.current) {
      return;
    }
    const trimmed = value.trim();
    if (trimmed === "" || trimmed === initialValue) {
      onCancel();
    } else {
      onSubmit(trimmed);
    }
  };

  const handleCancel = () => {
    isCancelled.current = true;
    onCancel();
  };

  // Select text on mount for rename
  const inputRef = useRef<HTMLInputElement>(null);
  useEffect(() => {
    if (inputRef.current) {
      inputRef.current.focus();
      if (initialValue) {
        inputRef.current.select();
      }
    }
  }, [initialValue]);

  const inputElement = (
    <input
      ref={inputRef}
      type="text"
      value={value}
      onChange={(e) => setValue(e.target.value)}
      className="h-5 flex-1 bg-card px-1 text-sm text-foreground outline-none ring-1 ring-primary"
      onBlur={handleSubmit}
      onKeyDown={(e) => {
        if (e.key === "Enter") {
          e.preventDefault();
          handleSubmit();
        } else if (e.key === "Escape") {
          e.preventDefault();
          handleCancel();
        }
      }}
      // Prevent drag events when selecting text
      draggable
      onDragStart={(e) => {
        e.preventDefault();
        e.stopPropagation();
      }}
      onClick={(e) => e.stopPropagation()}
      onDoubleClick={(e) => e.stopPropagation()}
    />
  );

  if (isRename) {
    return inputElement;
  }

  return (
    <div
      className={cn(
        "flex items-center gap-1.5 rounded-sm bg-muted px-2 py-1 text-sm",
      )}
      style={{ paddingLeft: `${paddingLeft}px` }}
    >
      {type === "folder" ? (
        <Folder size={14} className="shrink-0 text-primary" />
      ) : (
        <FileText size={14} className="shrink-0 text-muted-foreground" />
      )}
      {inputElement}
    </div>
  );
}
