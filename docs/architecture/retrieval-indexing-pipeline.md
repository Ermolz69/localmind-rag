# Retrieval and Indexing Pipeline

LocalMind keeps document processing local-first. The desktop app calls LocalApi, and LocalApi owns extraction, chunking, embedding generation, SQLite persistence, vector search, keyword search, and RAG context assembly.

## Baseline

The previous indexing baseline used fixed-size character chunking:

- Target chunk size: 1200 characters.
- Overlap: 0 characters.
- Embedding model: `bge-m3`.
- Embedding generation: sequential, one chunk per request.

This baseline remains useful for evaluation comparisons, but it is no longer the default chunking strategy.

## Extraction

Extraction produces ordered text segments before chunking:

- PDF extraction preserves page-level segment metadata where available.
- Markdown and plain text extraction preserve source order and textual structure.
- Office document extraction normalizes text into local document segments before indexing.

The ingestion job lifecycle remains durable. Upload and reindex create pending jobs, extraction happens inside LocalApi workers, and failed extraction keeps the original uploaded file.

## Structure-Aware Chunking

The default strategy is `StructureAware`.

Configuration:

```json
{
  "Chunking": {
    "Strategy": "StructureAware",
    "TargetChunkCharacters": 1200,
    "MaxChunkCharacters": 1600,
    "MinChunkCharacters": 200,
    "OverlapCharacters": 150,
    "ApplyOverlapOnlyOnForcedSplit": true,
    "PreserveHeadings": true
  }
}
```

`TargetChunkCharacters` is a target size, not a hard boundary. The chunker prefers semantic boundaries in this order:

1. Headings.
2. Paragraphs.
3. Sentences.
4. Forced character split.

Small adjacent blocks are merged until they approach the target size. Large blocks are split by sentence where possible. The configured overlap is applied only during forced character splits by default, so ordinary paragraph or heading boundaries do not duplicate text unnecessarily.

## Metadata

Chunks store stable display text and deterministic text hashes. Additional metadata is represented through chunk tags where available:

- `documentId`
- `chunkIndex`
- `headingPath`
- `pageNumber`
- `textHash`
- `sourceSpan`

`pageNumber` comes from extraction segments when a source format provides it. `sourceSpan` is a best-effort character span within the extracted segment.

## Embedding Text and Display Text

The UI displays the original chunk text. Embedding generation may include lightweight context such as the title or section path, but that contextual text is an indexing concern and should not replace the user-visible source snippet.

## Embedding Batching

Embedding generation uses `Ai:EmbeddingBatchSize`, defaulting to `16`. Batch-capable providers send multiple chunk texts in one OpenAI-compatible `/v1/embeddings` request with `input: string[]`.

The embedding service validates batch response count, preserves chunk order, stores vector dimensions, and falls back to single-request generation for providers that do not support batching. Logs include batch size, latency, chunks per second, and average latency per chunk.

## Retrieval

Current retrieval is intentionally conservative:

- Semantic retrieval uses exact vector scan over stored local embeddings.
- Keyword search is available separately over chunk text and tags.
- RAG context assembly uses the current semantic retrieval behavior and source score thresholds.

True hybrid ranking is not enabled yet. Semantic-only and keyword baselines should be compared separately in RAG evaluation tests until a later ADR or architecture update introduces hybrid ranking.

## Planned Work

Planned retrieval/indexing improvements:

- Hybrid retrieval that combines vector and keyword signals.
- Reranking for top candidate chunks.
- Token-based chunking experiments.
- Expanded heading-sensitive and long-document RAG evaluation fixtures.
