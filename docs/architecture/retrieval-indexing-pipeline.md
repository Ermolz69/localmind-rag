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

## Event-Driven Ingestion Dispatch

SQLite is the source of truth for ingestion jobs. After upload, reindex, watched-file ingestion, or retry commits a pending job, the producer publishes its job ID to an in-process `Channel<Guid>`. The single ingestion consumer wakes immediately and atomically claims the job before processing it.

The channel deduplicates queued and in-flight job IDs. It is an acceleration mechanism rather than durable storage: at startup and every `IngestionWorker:RecoveryIntervalSeconds` seconds, the worker queries up to `IngestionWorker:RecoveryBatchSize` pending jobs from SQLite. The defaults are 60 seconds and 100 jobs. This recovery pass covers process restarts, publication failures after commit, and future producers that persist work without an in-memory signal.

## Structure-Aware Chunking

The default algorithm is `structure-aware-token-v3`.

Configuration:

```json
{
  "Chunking": {
    "ChunkingVersion": 3,
    "ChunkingAlgorithmId": "structure-aware-token-v3",
    "Default": {
      "TargetTokens": 300,
      "MaxTokens": 450,
      "MinTokens": 80,
      "OverlapTokens": 40
    }
  }
}
```

`TargetTokens` is the preferred size and `MaxTokens` is the hard emitted chunk limit. The chunker prefers semantic boundaries in this order:

1. Headings.
2. Paragraphs.
3. Forced token-window split for oversized blocks.

Small adjacent blocks are merged until they approach the target size, while chunks that are still under `MinTokens` may grow up to `MaxTokens`. Oversized blocks are split by tokenizer spans from the original extracted text using `TargetTokens` windows and `OverlapTokens` stride. Overlap is applied only to forced splits, so ordinary paragraph or heading boundaries do not duplicate text unnecessarily.

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
- Chunk profile quality evaluation across document types.
- Expanded heading-sensitive and long-document RAG evaluation fixtures.
