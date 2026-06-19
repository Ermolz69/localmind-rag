import {
  Calendar,
  ChevronDown,
  Database,
  FileType,
  Filter,
  Globe2,
  Loader2,
  Plus,
  Search,
  Send,
  SlidersHorizontal,
  X,
} from "lucide-react";
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
  const [isActionMenuOpen, setIsActionMenuOpen] = useState(false);
  const [prevQuery, setPrevQuery] = useState("");

  const textareaRef = useRef<HTMLTextAreaElement>(null);

  const autocomplete = getAutocompleteContext(value, cursorPos, buckets, []);
  const showSuggestions =
    autocomplete.type !== "none" &&
    autocomplete.suggestions.length > 0 &&
    !isDismissed;
  const commandActions = [
    {
      icon: Database,
      label: "Scope",
      description: "Filter by bucket",
      command: "/bucket ",
    },
    {
      icon: FileType,
      label: "File type",
      description: "Limit by format",
      command: "/file-type ",
    },
    {
      icon: Calendar,
      label: "Date",
      description: "Use a date range",
      command: "/date ",
    },
  ];

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

  useEffect(() => {
    const textarea = textareaRef.current;
    if (!textarea) {
      return;
    }

    textarea.style.height = "auto";
    textarea.style.height = `${Math.min(textarea.scrollHeight, 168)}px`;
  }, [value]);

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

  const insertCommand = useCallback(
    (command: string) => {
      const current = value.trimEnd();
      const prefix = current.length > 0 ? `${current}\n` : "";
      const nextValue = `${prefix}${command}`;
      onChange(nextValue);

      setTimeout(() => {
        textareaRef.current?.focus();
        const nextCursor = nextValue.length;
        textareaRef.current?.setSelectionRange(nextCursor, nextCursor);
        setCursorPos(nextCursor);
      }, 0);
      setIsActionMenuOpen(false);
    },
    [onChange, value],
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
    <form
      className="pointer-events-none absolute inset-x-0 bottom-0 z-20 px-5 pb-4"
      onSubmit={submit}
    >
      <div className="pointer-events-auto relative mx-auto w-full max-w-4xl">
        {showSuggestions && (
          <div className="absolute bottom-full left-0 right-0 z-50 mb-2 flex max-h-60 flex-col gap-0.5 overflow-y-auto rounded-lg border border-border bg-card p-1.5 shadow-lg">
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
                  <span className="inline-flex min-w-0 items-center gap-2">
                    <Search size={14} aria-hidden />
                    <span className="truncate">{suggestion.text}</span>
                  </span>
                  {suggestion.description && (
                    <span className="ml-3 shrink-0 text-xs text-muted-foreground opacity-80">
                      {suggestion.description}
                    </span>
                  )}
                </button>
              );
            })}
          </div>
        )}
        <div className="relative flex w-full flex-col gap-2 rounded-xl border border-border bg-gradient-to-br from-background/95 via-card/95 to-background/95 p-3 shadow-2xl backdrop-blur transition-all focus-within:border-primary/60 focus-within:ring-2 focus-within:ring-primary/20">
          {filters.length > 0 ? (
            <div className="flex flex-wrap gap-2">
              {filters.map((filter) => (
                <span
                  key={filter.key}
                  className="inline-flex max-w-full items-center gap-1.5 rounded-md border border-border bg-muted px-2 py-1 text-xs font-medium text-muted-foreground"
                >
                  <SlidersHorizontal size={12} aria-hidden />
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
          <div className="flex w-full items-start gap-3">
            <textarea
              ref={textareaRef}
              className="max-h-40 min-h-7 flex-1 resize-none overflow-y-auto bg-transparent py-0.5 text-sm leading-6 text-foreground outline-none placeholder:text-muted-foreground"
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
              placeholder="Ask a grounded question. Use / to filter sources or @ to mention."
              rows={1}
              disabled={disabled}
            />
            <Button
              type={isSending ? "button" : "submit"}
              className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg p-0 shadow-sm"
              disabled={isSending ? false : !value.trim() || disabled}
              onClick={isSending ? onCancel : undefined}
              title={isSending ? "Stop response" : "Send message"}
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
          <div className="flex items-center justify-between gap-3 text-[11px] text-muted-foreground">
            <div className="relative flex min-w-0 items-center gap-2">
              <button
                type="button"
                className="flex h-8 w-8 shrink-0 items-center justify-center rounded-md border border-border bg-background/70 transition hover:border-primary/40 hover:text-foreground"
                onClick={() => setIsActionMenuOpen((current) => !current)}
                title="Add filter or source"
              >
                <Plus size={14} aria-hidden />
              </button>
              {isActionMenuOpen ? (
                <div className="absolute bottom-full left-0 z-50 mb-2 w-52 rounded-lg border border-border bg-card p-1.5 shadow-xl">
                  {commandActions.map((action) => {
                    const Icon = action.icon;
                    return (
                      <button
                        key={action.label}
                        type="button"
                        className="flex w-full items-center gap-2 rounded-md px-2.5 py-2 text-left text-xs text-muted-foreground transition hover:bg-muted hover:text-foreground"
                        onClick={() => insertCommand(action.command)}
                      >
                        <Icon size={14} aria-hidden />
                        <span className="min-w-0 flex-1">
                          <span className="block font-medium text-foreground">
                            {action.label}
                          </span>
                          <span className="block truncate">
                            {action.description}
                          </span>
                        </span>
                      </button>
                    );
                  })}
                </div>
              ) : null}
              <button
                type="button"
                className="inline-flex h-8 items-center gap-1.5 rounded-md border border-border bg-background/70 px-2.5 transition hover:border-primary/40 hover:text-foreground"
                onClick={() => insertCommand("/bucket ")}
              >
                <Globe2 size={13} aria-hidden />
                All sources
                <ChevronDown size={12} aria-hidden />
              </button>
              <button
                type="button"
                className="inline-flex h-8 items-center gap-1.5 rounded-md border border-border bg-background/70 px-2.5 transition hover:border-primary/40 hover:text-foreground"
                onClick={() => insertCommand("/tag ")}
              >
                <Filter size={13} aria-hidden />
                Filters
              </button>
              <span className="hidden truncate sm:inline">
                Press Enter to send
              </span>
            </div>
            <span className="shrink-0 tabular-nums">
              {value.trim().length} / 4000
            </span>
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
