import { Search } from "lucide-react";

import { Button, Input } from "@shared/ui";

import { useCompanionSearch } from "../model/useCompanionSearch";

export function CompanionSearch() {
  const { query, setQuery, results, isSearching, error, runSearch } =
    useCompanionSearch();

  function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    void runSearch();
  }

  return (
    <div className="flex min-h-0 flex-1 flex-col gap-4">
      <form onSubmit={handleSubmit} className="flex gap-2">
        <Input
          value={query}
          onChange={(event) => setQuery(event.target.value)}
          placeholder="Search your knowledge base"
          aria-label="Search query"
          className="flex-1"
        />
        <Button type="submit" disabled={isSearching || !query.trim()}>
          <Search className="h-4 w-4" />
          {isSearching ? "…" : "Search"}
        </Button>
      </form>

      {error ? <p className="text-destructive text-sm">{error}</p> : null}

      <div className="flex min-h-0 flex-1 flex-col gap-3 overflow-y-auto">
        {isSearching ? (
          <p className="text-sm text-muted-foreground">Searching…</p>
        ) : results === null ? (
          <p className="text-sm text-muted-foreground">
            Type a question or phrase to find matching snippets.
          </p>
        ) : results.length === 0 ? (
          <p className="text-sm text-muted-foreground">
            No matches found. Try a broader query.
          </p>
        ) : (
          results.map((source) => (
            <article
              key={source.chunkId}
              className="rounded-xl border border-border bg-card p-4"
            >
              <p className="text-sm font-medium text-foreground">
                {source.documentName}
              </p>
              <p className="mt-0.5 text-xs text-muted-foreground">
                {source.pageNumber
                  ? `Page ${source.pageNumber}`
                  : "Page not available"}
              </p>
              <p className="mt-2 whitespace-pre-wrap text-sm text-muted-foreground">
                {source.snippet}
              </p>
            </article>
          ))
        )}
      </div>
    </div>
  );
}
