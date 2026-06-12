import { Loader2, Send, X } from "lucide-react";
import { useState, useRef, useEffect, useCallback } from "react";
import type { FormEvent, KeyboardEvent } from "react";
import { Button } from "@shared/ui";
import { cn } from "@shared/lib/cn";
import type { BucketDto } from "@entities/bucket";
import { getAutocompleteContext } from "../model";
import type { SearchFilterChip, SearchFilterKey } from "../model";

type MessageComposerProps = {
  value: string;
  disabled: boolean;
  isSending?: boolean;
  error?: string | null;
  filters: SearchFilterChip[];
  buckets: BucketDto[];
  onChange: (value: string) => void;
  onRemoveFilter: (key: SearchFilterKey, tagKey?: string) => void;
  onSubmit: () => void;
  onCancel?: () => void;
};

export function MessageComposer({
  value,
  disabled,
  isSending,
  error,
  filters,
  buckets,
  onChange,
  onRemoveFilter,
  onSubmit,
  onCancel,
}: MessageComposerProps) {
  const [cursorPos, setCursorPos] = useState(0);
  const [activeSuggestionIndex, setActiveSuggestionIndex] = useState(0);
  const [isDismissed, setIsDismissed] = useState(false);
  const [prevQuery, setPrevQuery] = useState("");

  const textareaRef = useRef<HTMLTextAreaElement>(null);

  const autocomplete = getAutocompleteContext(value, cursorPos, buckets, []);
  const showSuggestions =
    autocomplete.type !== "none" &&
    autocomplete.suggestions.length > 0 &&
    !isDismissed;

  useEffect(() => {
    if (autocomplete.query !== prevQuery) {
      setIsDismissed(false);
      setPrevQuery(autocomplete.query);
      setActiveSuggestionIndex(0);
    }
  }, [autocomplete.query, prevQuery]);

  useEffect(() => {
    if (activeSuggestionIndex >= autocomplete.suggestions.length) {
      setActiveSuggestionIndex(0);
    }
  }, [autocomplete.suggestions.length, activeSuggestionIndex]);

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (value.trim() && !disabled) {
      onSubmit();
    }
  }

  const selectSuggestion = useCallback(
    (suggestionText: string) => {
      if (autocomplete.type === "none") return;

      const before = value.slice(0, autocomplete.startIndex);
      const after = value.slice(autocomplete.endIndex);

      const inserted = suggestionText + " ";
      const newValue = before + inserted + after;

      onChange(newValue);

      setTimeout(() => {
        if (textareaRef.current) {
          textareaRef.current.focus();
          const newCursorPos = autocomplete.startIndex + inserted.length;
          textareaRef.current.setSelectionRange(newCursorPos, newCursorPos);
          setCursorPos(newCursorPos);
        }
      }, 0);

      setActiveSuggestionIndex(0);
    },
    [value, autocomplete, onChange],
  );

  function handleKeyDown(event: KeyboardEvent<HTMLTextAreaElement>) {
    if (showSuggestions) {
      if (event.key === "ArrowDown") {
        event.preventDefault();
        setActiveSuggestionIndex((prev) =>
          prev < autocomplete.suggestions.length - 1 ? prev + 1 : 0,
        );
        return;
      }
      if (event.key === "ArrowUp") {
        event.preventDefault();
        setActiveSuggestionIndex((prev) =>
          prev > 0 ? prev - 1 : autocomplete.suggestions.length - 1,
        );
        return;
      }
      if (event.key === "Enter") {
        event.preventDefault();
        const activeSuggestion =
          autocomplete.suggestions[activeSuggestionIndex];
        if (activeSuggestion) {
          selectSuggestion(activeSuggestion.text);
        }
        return;
      }
      if (event.key === "Escape") {
        event.preventDefault();
        setIsDismissed(true);
        return;
      }
    }

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
        {showSuggestions && (
          <div className="absolute bottom-full left-0 right-0 z-50 mb-2 flex max-h-60 flex-col gap-0.5 overflow-y-auto rounded-xl border border-border bg-card p-1.5 shadow-lg">
            {autocomplete.suggestions.map((suggestion, index) => {
              const isActive = index === activeSuggestionIndex;
              return (
                <button
                  key={suggestion.text}
                  type="button"
                  className={cn(
                    "flex w-full items-center justify-between rounded-lg px-3 py-2 text-left text-sm transition-colors",
                    isActive
                      ? "bg-muted font-medium text-foreground"
                      : "text-muted-foreground hover:bg-muted/50 hover:text-foreground",
                  )}
                  onClick={() => selectSuggestion(suggestion.text)}
                >
                  <span>{suggestion.text}</span>
                  {suggestion.description && (
                    <span className="text-xs text-muted-foreground opacity-80">
                      {suggestion.description}
                    </span>
                  )}
                </button>
              );
            })}
          </div>
        )}
        <div className="relative flex w-full flex-col gap-2 rounded-2xl border border-border bg-background px-3 py-2 shadow-sm transition-all focus-within:border-primary focus-within:ring-2 focus-within:ring-primary/20">
          {filters.length > 0 ? (
            <div className="flex flex-wrap gap-2">
              {filters.map((filter) => (
                <span
                  key={filter.key}
                  className="inline-flex max-w-full items-center gap-1.5 rounded-md border border-border bg-muted/50 px-2 py-1 text-xs font-medium text-muted-foreground"
                >
                  <span className="truncate">{filter.label}</span>
                  <button
                    type="button"
                    className="rounded-sm text-muted-foreground transition hover:text-foreground"
                    onClick={() => onRemoveFilter(filter.key, filter.tagKey)}
                    title={`Remove ${filter.label}`}
                  >
                    <X size={13} aria-hidden />
                  </button>
                </span>
              ))}
            </div>
          ) : null}
          <div className="flex w-full items-end gap-2">
            <textarea
              ref={textareaRef}
              className="max-h-40 min-h-[24px] flex-1 resize-none bg-transparent py-1 text-sm leading-6 text-foreground outline-none placeholder:text-muted-foreground"
              value={value}
              onChange={(event) => {
                onChange(event.target.value);
                setCursorPos(event.target.selectionStart);
              }}
              onKeyDown={handleKeyDown}
              onClick={(event) =>
                setCursorPos(event.currentTarget.selectionStart)
              }
              onKeyUp={(event) =>
                setCursorPos(event.currentTarget.selectionStart)
              }
              onFocus={(event) =>
                setCursorPos(event.currentTarget.selectionStart)
              }
              placeholder="Ask your documents..."
              rows={1}
              disabled={disabled}
            />
            <Button
              type={isSending ? "button" : "submit"}
              className="mb-0.5 flex h-8 w-8 shrink-0 items-center justify-center rounded-lg p-0"
              disabled={isSending ? false : !value.trim() || disabled}
              onClick={isSending ? onCancel : undefined}
            >
              {isSending ? (
                <X size={18} aria-hidden />
              ) : disabled ? (
                <Loader2 size={18} className="animate-spin" aria-hidden />
              ) : (
                <Send size={18} aria-hidden />
              )}
            </Button>
          </div>
        </div>
        {error ? (
          <p className="border-destructive/20 bg-destructive/10 text-destructive mt-2 rounded-md border px-3 py-2 text-xs font-medium">
            {error}
          </p>
        ) : null}
      </div>
    </form>
  );
}
