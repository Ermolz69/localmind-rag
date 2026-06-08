using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Abstractions.Ingestion;
using KnowledgeApp.Application.Common.Diagnostics;
using KnowledgeApp.Application.Ingestion.IncrementalIndexing;
using KnowledgeApp.Infrastructure.Options;
using Microsoft.Extensions.Options;

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
}
