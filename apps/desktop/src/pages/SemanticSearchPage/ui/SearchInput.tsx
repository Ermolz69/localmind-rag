import { useState, useRef, useEffect, useCallback } from "react";
import type { KeyboardEvent } from "react";
import { X } from "lucide-react";
import type { BucketDto } from "@entities/bucket";
import type { DocumentSummary } from "@entities/document";
import type { SearchFilterChip, SearchFilterKey } from "@entities/search";
import { getAutocompleteContext } from "@shared/lib/searchFilterCommands";
import { cn } from "@shared/lib/cn";

type SearchInputProps = {
  id?: string;
  className?: string;
  value: string;
  placeholder?: string;
  filters: SearchFilterChip[];
  buckets: BucketDto[];
  documents: DocumentSummary[];
  onChange: (value: string) => void;
  onRemoveFilter: (key: SearchFilterKey, tagKey?: string) => void;
  onSubmit: () => void;
};

export function SearchInput({
  id,
  className,
  value,
  placeholder,
  filters,
  buckets,
  documents,
  onChange,
  onRemoveFilter,
  onSubmit,
}: SearchInputProps) {
  const [cursorPos, setCursorPos] = useState(0);
  const [activeSuggestionIndex, setActiveSuggestionIndex] = useState(0);
  const [isDismissed, setIsDismissed] = useState(false);
  const [prevQuery, setPrevQuery] = useState("");

  const inputRef = useRef<HTMLInputElement>(null);

  const autocomplete = getAutocompleteContext(
    value,
    cursorPos,
    buckets,
    documents,
  );
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

  const selectSuggestion = useCallback(
    (suggestionText: string) => {
      if (autocomplete.type === "none") return;

      const before = value.slice(0, autocomplete.startIndex);
      const after = value.slice(autocomplete.endIndex);

      const inserted = suggestionText + " ";
      const newValue = before + inserted + after;

      onChange(newValue);

      setTimeout(() => {
        if (inputRef.current) {
          inputRef.current.focus();
          const newCursorPos = autocomplete.startIndex + inserted.length;
          inputRef.current.setSelectionRange(newCursorPos, newCursorPos);
          setCursorPos(newCursorPos);
        }
      }, 0);

      setActiveSuggestionIndex(0);
    },
    [value, autocomplete, onChange],
  );

  function handleKeyDown(event: KeyboardEvent<HTMLInputElement>) {
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

    if (event.key === "Enter") {
      event.preventDefault();
      if (value.trim()) {
        onSubmit();
      }
    }
  }

  return (
    <div
      className={cn(
        "border-input relative flex w-full flex-col gap-2 rounded-md border bg-background px-3 py-2 shadow-sm transition-colors focus-within:border-primary focus-within:ring-1 focus-within:ring-primary",
        className,
      )}
    >
      {showSuggestions && (
        <div className="absolute bottom-full left-0 right-0 z-50 mb-1 flex max-h-60 flex-col gap-0.5 overflow-y-auto rounded-md border border-border bg-card p-1 shadow-md">
          {autocomplete.suggestions.map((suggestion, index) => {
            const isActive = index === activeSuggestionIndex;
            return (
              <button
                key={suggestion.text}
                type="button"
                className={cn(
                  "flex w-full items-center justify-between rounded-sm px-2 py-1.5 text-left text-sm transition-colors",
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

      {filters.length > 0 ? (
        <div className="flex flex-wrap gap-1.5">
          {filters.map((filter) => (
            <span
              key={`${filter.key}-${filter.tagKey ?? ""}`}
              className="inline-flex max-w-full items-center gap-1 rounded border border-border bg-muted/50 px-2 py-0.5 text-xs font-medium text-muted-foreground"
            >
              <span className="truncate">{filter.label}</span>
              <button
                type="button"
                className="rounded-sm text-muted-foreground transition hover:text-foreground"
                onClick={() => onRemoveFilter(filter.key, filter.tagKey)}
                title={`Remove ${filter.label}`}
              >
                <X size={12} aria-hidden />
              </button>
            </span>
          ))}
        </div>
      ) : null}

      <input
        ref={inputRef}
        id={id}
        className="flex-1 bg-transparent text-sm text-foreground outline-none placeholder:text-muted-foreground"
        value={value}
        onChange={(event) => {
          onChange(event.target.value);
          setCursorPos(event.target.selectionStart ?? 0);
        }}
        onKeyDown={handleKeyDown}
        onClick={(event) =>
          setCursorPos(event.currentTarget.selectionStart ?? 0)
        }
        onKeyUp={(event) =>
          setCursorPos(event.currentTarget.selectionStart ?? 0)
        }
        onFocus={(event) =>
          setCursorPos(event.currentTarget.selectionStart ?? 0)
        }
        placeholder={placeholder}
        autoComplete="off"
      />
    </div>
  );
}
