import { Search, Sparkles, SlidersHorizontal } from "lucide-react";
import { useSemanticSearchPageViewModel } from "./model/useSemanticSearchPageViewModel";
import {
  Button,
  EmptyState,
  ErrorBanner,
  PageHeader,
  Select,
  Toolbar,
} from "@shared/ui";
import { SearchInput } from "./ui/SearchInput";

export function SemanticSearchPage() {
  const page = useSemanticSearchPageViewModel();

  return (
    <section className="flex min-h-[calc(100dvh-5.5rem)] flex-col space-y-4">
      <PageHeader
        title="Semantic search"
        description="Search indexed document chunks and filter the results by bucket."
        actions={
          <Button variant="secondary" onClick={page.clearSearch}>
            <Sparkles size={16} aria-hidden />
            Reset
          </Button>
        }
      />

      <ErrorBanner message={page.error} />

      <div className="rounded-md border border-border bg-card p-3 shadow-sm">
        <Toolbar className="items-end gap-2 border-0 bg-transparent p-0">
          <SearchInput
            id="semantic-search-query"
            className="min-w-0 flex-1"
            value={page.query}
            onChange={page.setQuery}
            onSubmit={() => void page.runSearch()}
            placeholder="Search snippets (type / for filters)"
            filters={page.activeFilterChips}
            buckets={page.buckets.buckets}
            documents={page.documents.documents}
            onRemoveFilter={page.removeActiveFilter}
          />

          <Select
            id="semantic-search-bucket"
            className="max-w-56"
            value={page.selectedBucketId}
            onChange={(event) => page.setSelectedBucketId(event.target.value)}
            aria-label="Filter by bucket"
          >
            <option value="">All buckets</option>
            {page.buckets.buckets.map((bucket) => (
              <option key={bucket.id} value={bucket.id}>
                {bucket.name}
              </option>
            ))}
          </Select>

          <Button
            className="!h-11 min-w-28 shrink-0 !px-5 !text-sm !font-medium"
            onClick={() => void page.runSearch()}
            disabled={!page.query.trim()}
          >
            <Search size={16} aria-hidden />
            Search
          </Button>
        </Toolbar>

        <p className="mt-2 text-xs text-muted-foreground">
          Type a query, optionally narrow by bucket, then run the search.
        </p>
      </div>

      <div className="rounded-md border border-border bg-card p-4 shadow-sm">
        <div className="flex flex-wrap items-center justify-between gap-2 border-b border-border pb-3 text-sm text-muted-foreground">
          <span>
            {page.searchSubmitted
              ? `${page.results.length} snippet${page.results.length === 1 ? "" : "s"} found`
              : "Enter a search to find indexed snippets."}
          </span>
          <span className="flex items-center gap-2">
            <SlidersHorizontal size={14} aria-hidden />
            {page.selectedBucketName}
          </span>
        </div>

        <div className="mt-4 min-h-72">
          {page.isSearching ? (
            <div className="flex min-h-72 items-center justify-center rounded-md border border-dashed border-border bg-muted/30 text-sm text-muted-foreground">
              Searching indexed chunks...
            </div>
          ) : page.searchSubmitted && page.results.length > 0 ? (
            <div className="space-y-3">
              {page.results.map((source) => (
                <article
                  key={source.chunkId}
                  className="rounded-md border border-border bg-background p-4 shadow-sm transition-colors hover:bg-muted/30"
                >
                  <div className="flex flex-wrap items-start justify-between gap-3">
                    <div>
                      <div className="text-sm font-semibold text-foreground">
                        {source.documentName}
                      </div>
                      <div className="mt-1 text-xs text-muted-foreground">
                        {source.pageNumber
                          ? `Page ${source.pageNumber}`
                          : "Page not available"}
                      </div>
                    </div>
                    <div className="rounded-full bg-primary/10 px-3 py-1 text-xs font-medium text-primary">
                      Score {source.score.toFixed(4)}
                    </div>
                  </div>

                  <p className="mt-3 whitespace-pre-wrap text-sm leading-6 text-muted-foreground">
                    {source.snippet}
                  </p>
                </article>
              ))}
            </div>
          ) : page.searchSubmitted ? (
            <EmptyState
              icon={<Search size={20} aria-hidden />}
              title="No snippets found"
              description="Try a broader query or switch to another bucket to surface indexed chunks."
            />
          ) : (
            <EmptyState
              icon={<Search size={20} aria-hidden />}
              title="Search indexed documents"
              description="Type a question or phrase to search chunk snippets from indexed documents."
            />
          )}
        </div>
      </div>
    </section>
  );
}
