import { Loader2, Send } from "lucide-react";
import type { FormEvent, KeyboardEvent } from "react";
import { Button } from "@shared/ui";

type MessageComposerProps = {
  value: string;
  disabled: boolean;
  onChange: (value: string) => void;
  onSubmit: () => void;
};

export function MessageComposer({
  value,
  disabled,
  onChange,
  onSubmit,
}: MessageComposerProps) {
  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (value.trim() && !disabled) {
      onSubmit();
    }
  }

  function handleKeyDown(event: KeyboardEvent<HTMLTextAreaElement>) {
    if (event.key === "Enter" && !event.shiftKey) {
      event.preventDefault();
      if (value.trim() && !disabled) {
        onSubmit();
      }
    }
  }

  return (
    <form className="w-full bg-transparent px-4 pb-0 pt-2" onSubmit={submit}>
      <div className="relative mx-auto w-full max-w-3xl">
        <div className="relative flex w-full items-end gap-2 rounded-2xl border border-border bg-background py-2 pl-4 pr-2 shadow-sm transition-all focus-within:border-primary focus-within:ring-2 focus-within:ring-primary/20">
          <textarea
            className="max-h-40 min-h-[24px] flex-1 resize-none bg-transparent py-1 text-sm leading-6 text-foreground outline-none placeholder:text-muted-foreground"
            value={value}
            onChange={(event) => onChange(event.target.value)}
            onKeyDown={handleKeyDown}
            placeholder="Ask your documents..."
            rows={1}
            disabled={disabled}
          />
          <Button
            type="submit"
            className="mb-0.5 flex h-8 w-8 shrink-0 items-center justify-center rounded-lg p-0"
            disabled={!value.trim() || disabled}
          >
            {disabled ? (
              <Loader2 size={18} className="animate-spin" aria-hidden />
            ) : (
              <Send size={18} aria-hidden />
            )}
          </Button>
        </div>
      </div>
    </form>
  );
}
