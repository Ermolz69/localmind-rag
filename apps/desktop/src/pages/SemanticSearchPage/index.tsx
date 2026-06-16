import { Search, Sparkles, SlidersHorizontal } from "lucide-react";
import type { ContentScope } from "./model/useSemanticSearchPageViewModel";
import { useSemanticSearchPageViewModel } from "./model/useSemanticSearchPageViewModel";
import {
  Button,
  EmptyState,
  ErrorBanner,
  PageHeader,
  Select,
  Toolbar,
  Tooltip,
} from "@shared/ui";
import { SearchInput } from "./ui/SearchInput";
import { cn } from "@shared/lib/cn";

export function SemanticSearchPage() {
  const page = useSemanticSearchPageViewModel();

  return (
    <section className="flex min-h-[calc(100dvh-5.5rem)] flex-col space-y-4">
      <PageHeader
        title="Search"
        description="Search your local knowledge base using AI-powered semantic matching or exact text search."
        actions={
          <Button variant="secondary" onClick={page.clearSearch}>
            <Sparkles size={16} aria-hidden />
            Reset
          </Button>
        }
      />

      <ErrorBanner message={page.error} />

      <div className="mb-2 flex w-fit space-x-1 rounded-lg bg-muted/30 p-1">
        <button
          className={cn(
            "rounded-md px-3 py-1.5 text-sm font-medium transition-colors",
            page.searchMode === "semantic"
              ? "bg-background text-foreground shadow"
              : "text-muted-foreground hover:bg-muted/50 hover:text-foreground",
          )}
          onClick={() => page.setSearchMode("semantic")}
        >
          Semantic Search
        </button>
        <button
          className={cn(
            "rounded-md px-3 py-1.5 text-sm font-medium transition-colors",
            page.searchMode === "content"
              ? "bg-background text-foreground shadow"
              : "text-muted-foreground hover:bg-muted/50 hover:text-foreground",
          )}
          onClick={() => page.setSearchMode("content")}
        >
          Content Search
        </button>
      </div>

      <div className="rounded-md border border-border bg-card p-3 shadow-sm">
        <Toolbar className="items-end gap-2 border-0 bg-transparent p-0">
          <SearchInput
            id="semantic-search-query"
            className="min-w-0 flex-1"
            value={page.query}
            onChange={page.setQuery}
            onSubmit={() => void page.runSearch()}
            placeholder={
              page.searchMode === "semantic"
                ? "Search by meaning (type / for filters)"
                : "Search exact text (type / for filters)"
            }
            filters={page.activeFilterChips}
            buckets={page.buckets.buckets}
            documents={page.documents.documents}
            onRemoveFilter={page.removeActiveFilter}
          />

          {page.searchMode === "content" && (
            <Select
              id="content-search-scope"
              className="max-w-40"
              value={page.contentScope}
              onChange={(event) =>
                page.setContentScope(event.target.value as ContentScope)
              }
              aria-label="Filter content scope"
            >
              <option value="all">Docs + Notes</option>
              <option value="documents">Docs only</option>
              <option value="notes">Notes only</option>
            </Select>
          )}

          <Select
            id="search-bucket"
            className="max-w-40"
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
          {page.searchMode === "content"
            ? "Content search finds exact text matches across documents and notes. Useful for names, IDs, phrases, and keywords."
            : "Type a query, optionally narrow by bucket and filters, then run the search."}
        </p>
      </div>

      <div className="rounded-md border border-border bg-card p-4 shadow-sm">
        <div className="flex flex-wrap items-center justify-between gap-2 border-b border-border pb-3 text-sm text-muted-foreground">
          <span>
            {page.searchSubmitted
              ? `${
                  page.searchMode === "semantic"
                    ? page.semanticResults.length
                    : page.contentResults.length
                } snippet${
                  (page.searchMode === "semantic"
                    ? page.semanticResults.length
                    : page.contentResults.length) === 1
                    ? ""
                    : "s"
                } found`
              : "Enter a search to find snippets."}
          </span>
          <Tooltip content={page.selectedBucketName} className="flex min-w-0 max-w-[150px] sm:max-w-[250px] md:max-w-[350px]">
            <span className="flex w-full min-w-0 items-center gap-2">
              <SlidersHorizontal size={14} className="shrink-0" aria-hidden />
              <span className="truncate">{page.selectedBucketName}</span>
            </span>
          </Tooltip>
        </div>

        <div className="mt-4 min-h-72">
          {page.isSearching ? (
            <div className="flex min-h-72 items-center justify-center rounded-md border border-dashed border-border bg-muted/30 text-sm text-muted-foreground">
              Searching...
            </div>
          ) : page.searchSubmitted &&
            page.searchMode === "content" &&
            page.contentResults.length > 0 ? (
            <div className="space-y-3">
              {page.contentResults.map((hit) => (
                <article
                  key={`${hit.sourceType}-${hit.chunkId}`}
                  className="rounded-md border border-border bg-background p-4 shadow-sm transition-colors hover:bg-muted/30"
                >
                  <div className="flex flex-wrap items-start justify-between gap-3">
                    <div>
                      <div className="flex items-center gap-2">
                        <span className="rounded bg-muted px-1.5 py-0.5 text-[10px] font-bold uppercase tracking-wider text-muted-foreground">
                          {hit.sourceType}
                        </span>
                        <div className="text-sm font-semibold text-foreground">
                          {hit.title}
                        </div>
                      </div>
                      <div className="mt-1 text-xs text-muted-foreground">
                        {hit.pageNumber
                          ? `Page ${hit.pageNumber}`
                          : hit.sourceType === "Document"
                            ? "Page not available"
                            : "Note match"}
                      </div>
                    </div>
                    <div className="rounded-full bg-primary/10 px-3 py-1 text-xs font-medium text-primary">
                      Score {hit.score.toFixed(4)}
                    </div>
                  </div>

                  <p className="mt-3 whitespace-pre-wrap text-sm leading-6 text-muted-foreground">
                    {hit.snippet}
                  </p>
                </article>
              ))}
            </div>
          ) : page.searchSubmitted &&
            page.searchMode === "semantic" &&
            page.semanticResults.length > 0 ? (
            <div className="space-y-3">
              {page.semanticResults.map((source) => (
                <article
                  key={source.chunkId}
                  className="rounded-md border border-border bg-background p-4 shadow-sm transition-colors hover:bg-muted/30"
                >
                  <div className="flex flex-wrap items-start justify-between gap-3">
                    <div>
                      <div className="flex items-center gap-2">
                        <span className="rounded bg-muted px-1.5 py-0.5 text-[10px] font-bold uppercase tracking-wider text-muted-foreground">
                          Document
                        </span>
                        <div className="text-sm font-semibold text-foreground">
                          {source.documentName}
                        </div>
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
              description="Try a broader query or switch to another bucket/scope."
            />
          ) : (
            <EmptyState
              icon={<Search size={20} aria-hidden />}
              title="Search your knowledge base"
              description="Type a question or phrase to search indexed documents and notes."
            />
          )}
        </div>
      </div>
    </section>
  );
}
