import { Upload } from "lucide-react";
import type { RefObject } from "react";
import { cn } from "@shared/lib/cn";

type DocumentDropzoneProps = {
  fileInputRef: RefObject<HTMLInputElement | null>;
  isDragging: boolean;
  onDraggingChange: (isDragging: boolean) => void;
  onFileSelected: (file: File) => void;
};

export function DocumentDropzone({
  fileInputRef,
  isDragging,
  onDraggingChange,
  onFileSelected,
}: DocumentDropzoneProps) {
  return (
    <label
      className={cn(
        "flex min-h-32 cursor-pointer flex-col items-center justify-center rounded-md border border-dashed border-border bg-card px-4 py-6 text-center transition",
        isDragging && "bg-muted",
      )}
      onDragOver={(event) => {
        event.preventDefault();
        onDraggingChange(true);
      }}
      onDragLeave={() => onDraggingChange(false)}
      onDrop={(event) => {
        event.preventDefault();
        onDraggingChange(false);
        const file = event.dataTransfer.files.item(0);
        if (file) {
          onFileSelected(file);
        }
      }}
    >
      <input
        ref={fileInputRef}
        className="hidden"
        type="file"
        onChange={(event) => {
          const file = event.target.files?.item(0);
          event.currentTarget.value = "";
          if (file) {
            onFileSelected(file);
          }
        }}
      />
      <Upload className="mb-3 text-muted-foreground" size={24} aria-hidden />
      <span className="text-sm font-medium">
        Drop a document here or choose a file
      </span>
      <span className="mt-1 text-xs text-muted-foreground">
        TXT, Markdown, HTML, PDF, DOCX and PPTX can be indexed locally.
      </span>
    </label>
  );
}
