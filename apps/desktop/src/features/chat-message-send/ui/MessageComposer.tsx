import { Loader2, Send } from "lucide-react";
import type { FormEvent } from "react";
import { Button } from "@shared/ui";
import { Textarea } from "@shared/ui";

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
    onSubmit();
  }

  return (
    <form className="border-t border-border p-3" onSubmit={submit}>
      <div className="flex gap-2">
        <Textarea
          className="min-h-[56px] flex-1 resize-none"
          value={value}
          onChange={(event) => onChange(event.target.value)}
          placeholder="Ask your documents"
          rows={2}
          disabled={disabled}
        />
        <Button
          type="submit"
          className="self-end"
          disabled={!value.trim() || disabled}
        >
          {disabled ? (
            <Loader2 size={16} className="animate-spin" aria-hidden />
          ) : (
            <Send size={16} aria-hidden />
          )}
          Ask
        </Button>
      </div>
    </form>
  );
}
