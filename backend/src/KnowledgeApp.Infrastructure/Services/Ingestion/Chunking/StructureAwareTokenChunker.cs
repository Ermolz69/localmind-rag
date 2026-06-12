using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Abstractions.Ingestion;
using KnowledgeApp.Application.Common.Diagnostics;
using KnowledgeApp.Application.Ingestion.IncrementalIndexing;
using KnowledgeApp.Infrastructure.Options;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;
using System.Linq;

namespace KnowledgeApp.Infrastructure.Services.Ingestion.Chunking;

public sealed class StructureAwareTokenChunker(
    ITokenizerService tokenizer,
    IChunkTextNormalizer normalizer,
    IEnumerable<ITextStructureParser> parsers,
    IContentHashService hashService,
    IOptionsMonitor<ChunkingOptions> options) : IDocumentChunker
{
    public IReadOnlyList<DocumentChunkText> SplitDetailed(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        ChunkingOptions config = options.CurrentValue;
        ITextStructureParser parser = parsers.FirstOrDefault(p => p.CanParse(text))
            ?? parsers.First();

        IReadOnlyList<DocumentBlock> blocks = parser.Parse(text);
        List<DocumentChunkText> chunks = [];

        // Simplified grouping logic: combine blocks until TargetTokens is reached
        List<DocumentBlock> currentChunkBlocks = [];
        int currentTokenCount = 0;

        foreach (DocumentBlock block in blocks)
        {
            // Oversized block handling – split using natural boundaries
            if (block.TokenCount > config.Default.MaxTokens)
            {
                // Flush any accumulated chunk first
                if (currentChunkBlocks.Count > 0)
                {
                    chunks.Add(BuildChunk(currentChunkBlocks, config));
                    currentChunkBlocks.Clear();
                    currentTokenCount = 0;
                }

                // Split the oversized block and emit each piece as its own chunk
                foreach (var split in SplitOversizedBlock(block, config))
                {
                    chunks.Add(BuildChunk([split], config));
                }
                continue; // move to next original block
            }

            // Normal overflow handling – start a new chunk when adding this block would exceed the limit
            if (currentTokenCount + block.TokenCount > config.Default.MaxTokens && currentChunkBlocks.Count > 0)
            {
                chunks.Add(BuildChunk(currentChunkBlocks, config));
                currentChunkBlocks.Clear();
                currentTokenCount = 0;
            }

            currentChunkBlocks.Add(block);
            currentTokenCount += block.TokenCount;
        }

        if (currentChunkBlocks.Count > 0)
        {
            chunks.Add(BuildChunk(currentChunkBlocks, config));
        }

        return chunks;
    }

    private DocumentChunkText BuildChunk(List<DocumentBlock> blocks, ChunkingOptions config)
    {
        string text = string.Join("\n\n", blocks.Select(b => b.Text));
        string normalized = normalizer.NormalizeForEmbedding(text);
        string identityNormalized = normalizer.NormalizeForIdentity(text);
        int tokenCount = tokenizer.CountTokens(normalized);

        string metadataKey = $"{blocks.FirstOrDefault()?.HeadingPath}|{blocks.FirstOrDefault()?.SectionTitle}|{blocks.FirstOrDefault()?.SourceStartOffset}";
        string chunkIdentityHash = hashService.ComputeChunkHash(identityNormalized + metadataKey + config.ChunkingAlgorithmId);
        string embeddingTextHash = hashService.ComputeChunkHash(normalized);

        return new DocumentChunkText(
            Text: text,
            CoreText: text,
            HasOverlap: false,
            HeadingPath: blocks.FirstOrDefault()?.HeadingPath,
            SectionTitle: blocks.FirstOrDefault()?.SectionTitle,
            ChunkType: "text",
            SourceStartOffset: blocks.FirstOrDefault()?.SourceStartOffset,
            SourceEndOffset: blocks.LastOrDefault()?.SourceEndOffset,
            TokenCount: tokenCount,
            TokenizerId: tokenizer.TokenizerId,
            ChunkingAlgorithmId: config.ChunkingAlgorithmId,
            ChunkIdentityHash: chunkIdentityHash,
            EmbeddingTextHash: embeddingTextHash
        );
    }
    private IEnumerable<DocumentBlock> SplitOversizedBlock(DocumentBlock block, ChunkingOptions config)
    {
        // 1️⃣ Try paragraph split (double newline)
        var paragraphs = block.Text.Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var para in paragraphs)
        {
            int paraTokens = tokenizer.CountTokens(para);
            if (paraTokens <= config.Default.MaxTokens)
            {
                yield return new DocumentBlock(
                    Type: block.Type,
                    Text: para,
                    HeadingPath: block.HeadingPath,
                    SectionTitle: block.SectionTitle,
                    SourceStartOffset: null,
                    SourceEndOffset: null,
                    TokenCount: paraTokens,
                    IsAtomic: block.IsAtomic);
                continue;
            }

            // 2️⃣ Sentence split (simple regex)
            var sentences = Regex.Split(para, @"(?<=[.!?])\\s+");
            foreach (var sentence in sentences)
            {
                int sentTokens = tokenizer.CountTokens(sentence);
                if (sentTokens <= config.Default.MaxTokens)
                {
                    yield return new DocumentBlock(
                        Type: block.Type,
                        Text: sentence,
                        HeadingPath: block.HeadingPath,
                        SectionTitle: block.SectionTitle,
                        SourceStartOffset: null,
                        SourceEndOffset: null,
                        TokenCount: sentTokens,
                        IsAtomic: block.IsAtomic);
                    continue;
                }

                // 3️⃣ Token‑based split fallback
                var tokenIds = tokenizer.Encode(sentence);
                int start = 0;
                while (start < tokenIds.Count)
                {
                    int length = Math.Min(config.Default.MaxTokens, tokenIds.Count - start);
                    var sliceIds = tokenIds.Skip(start).Take(length).ToArray();
                    string sliceText = tokenizer.Decode(sliceIds);
                    yield return new DocumentBlock(
                        Type: block.Type,
                        Text: sliceText,
                        HeadingPath: block.HeadingPath,
                        SectionTitle: block.SectionTitle,
                        SourceStartOffset: null,
                        SourceEndOffset: null,
                        TokenCount: length,
                        IsAtomic: block.IsAtomic);
                    start += length;
                }
            }
        }
    }

}

