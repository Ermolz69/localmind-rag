using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Abstractions.Ingestion;
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
        ChunkingProfile profile = config.Default;
        ITextStructureParser parser = parsers.FirstOrDefault(p => p.CanParse(text))
            ?? parsers.First();

        IReadOnlyList<DocumentBlock> blocks = parser.Parse(text);
        List<DocumentChunkText> chunks = [];
        List<DocumentBlock> currentChunkBlocks = [];
        int currentTokenCount = 0;

        foreach (DocumentBlock block in blocks)
        {
            if (block.TokenCount > profile.MaxTokens)
            {
                FlushCurrentChunk(chunks, currentChunkBlocks, config, ref currentTokenCount);

                foreach (ForcedSplitBlock split in SplitOversizedBlock(block, profile))
                {
                    chunks.Add(BuildChunk([split.Block], config, split.CoreText, split.HasOverlap));
                }

                continue;
            }

            int nextTokenCount = currentTokenCount + block.TokenCount;
            bool exceedsMaxTokens = nextTokenCount > profile.MaxTokens;
            bool passedTargetWithEnoughContent =
                currentTokenCount >= profile.MinTokens &&
                nextTokenCount > profile.TargetTokens;

            if (currentChunkBlocks.Count > 0 && (exceedsMaxTokens || passedTargetWithEnoughContent))
            {
                FlushCurrentChunk(chunks, currentChunkBlocks, config, ref currentTokenCount);
            }

            currentChunkBlocks.Add(block);
            currentTokenCount += block.TokenCount;
        }

        FlushCurrentChunk(chunks, currentChunkBlocks, config, ref currentTokenCount);

        return chunks;
    }

    private void FlushCurrentChunk(
        List<DocumentChunkText> chunks,
        List<DocumentBlock> currentChunkBlocks,
        ChunkingOptions config,
        ref int currentTokenCount)
    {
        if (currentChunkBlocks.Count == 0)
        {
            return;
        }

        chunks.Add(BuildChunk(currentChunkBlocks, config, coreText: null, hasOverlap: false));
        currentChunkBlocks.Clear();
        currentTokenCount = 0;
    }

    private DocumentChunkText BuildChunk(
        IReadOnlyList<DocumentBlock> blocks,
        ChunkingOptions config,
        string? coreText,
        bool hasOverlap)
    {
        string text = string.Join("\n\n", blocks.Select(block => block.Text));
        string normalized = normalizer.NormalizeForEmbedding(text);
        string identityNormalized = normalizer.NormalizeForIdentity(text);
        int tokenCount = tokenizer.CountTokens(normalized);

        DocumentBlock? firstBlock = blocks.FirstOrDefault();
        DocumentBlock? lastBlock = blocks.LastOrDefault();
        string metadataKey = $"{firstBlock?.HeadingPath}|{firstBlock?.SectionTitle}|{firstBlock?.SourceStartOffset}";
        string chunkIdentityHash = hashService.ComputeChunkHash(identityNormalized + metadataKey + config.ChunkingAlgorithmId);
        string embeddingTextHash = hashService.ComputeChunkHash(normalized);

        return new DocumentChunkText(
            Text: text,
            CoreText: coreText ?? text,
            HasOverlap: hasOverlap,
            HeadingPath: firstBlock?.HeadingPath,
            SectionTitle: firstBlock?.SectionTitle,
            ChunkType: "text",
            SourceStartOffset: firstBlock?.SourceStartOffset,
            SourceEndOffset: lastBlock?.SourceEndOffset,
            TokenCount: tokenCount,
            TokenizerId: tokenizer.TokenizerId,
            ChunkingAlgorithmId: config.ChunkingAlgorithmId,
            ChunkIdentityHash: chunkIdentityHash,
            EmbeddingTextHash: embeddingTextHash);
    }

    private IEnumerable<ForcedSplitBlock> SplitOversizedBlock(DocumentBlock block, ChunkingProfile profile)
    {
        IReadOnlyList<TokenSpan> tokenSpans = tokenizer.GetTokenSpans(block.Text);
        List<TokenSpan> expandedTokenSpans = ExpandTokenSpans(tokenSpans);

        if (expandedTokenSpans.Count == 0)
        {
            yield break;
        }

        int windowTokenCount = Math.Min(profile.TargetTokens, profile.MaxTokens);
        int overlapTokenCount = Math.Min(profile.OverlapTokens, Math.Max(0, windowTokenCount - 1));
        int strideTokenCount = Math.Max(1, windowTokenCount - overlapTokenCount);

        for (int startTokenIndex = 0; startTokenIndex < expandedTokenSpans.Count;)
        {
            int endTokenIndex = Math.Min(startTokenIndex + windowTokenCount, expandedTokenSpans.Count);
            TextSlice textSlice = CreateSlice(block.Text, expandedTokenSpans, startTokenIndex, endTokenIndex);

            if (!string.IsNullOrWhiteSpace(textSlice.Text))
            {
                bool hasOverlap = startTokenIndex > 0 && overlapTokenCount > 0;
                int coreStartTokenIndex = hasOverlap
                    ? Math.Min(startTokenIndex + overlapTokenCount, endTokenIndex)
                    : startTokenIndex;
                TextSlice coreSlice = coreStartTokenIndex < endTokenIndex
                    ? CreateSlice(block.Text, expandedTokenSpans, coreStartTokenIndex, endTokenIndex)
                    : textSlice;
                int? sourceStartOffset = AddOffset(block.SourceStartOffset, textSlice.StartIndex);
                int? sourceEndOffset = AddOffset(block.SourceStartOffset, textSlice.EndIndex);

                yield return new ForcedSplitBlock(
                    new DocumentBlock(
                        block.Type,
                        textSlice.Text,
                        block.HeadingPath,
                        block.SectionTitle,
                        sourceStartOffset,
                        sourceEndOffset,
                        endTokenIndex - startTokenIndex,
                        block.IsAtomic),
                    string.IsNullOrWhiteSpace(coreSlice.Text) ? textSlice.Text : coreSlice.Text,
                    hasOverlap);
            }

            if (endTokenIndex >= expandedTokenSpans.Count)
            {
                break;
            }

            startTokenIndex += strideTokenCount;
        }
    }

    private static List<TokenSpan> ExpandTokenSpans(IReadOnlyList<TokenSpan> tokenSpans)
    {
        List<TokenSpan> expanded = [];

        foreach (TokenSpan tokenSpan in tokenSpans)
        {
            int tokenCount = Math.Max(1, tokenSpan.TokenCount);

            for (int index = 0; index < tokenCount; index++)
            {
                expanded.Add(tokenSpan);
            }
        }

        return expanded;
    }

    private static TextSlice CreateSlice(
        string text,
        IReadOnlyList<TokenSpan> tokenSpans,
        int startTokenIndex,
        int endTokenIndex)
    {
        TokenSpan firstToken = tokenSpans[startTokenIndex];
        TokenSpan lastToken = tokenSpans[endTokenIndex - 1];
        int startIndex = Math.Clamp(firstToken.StartIndex, 0, text.Length);
        int endIndex = Math.Clamp(lastToken.StartIndex + lastToken.Length, startIndex, text.Length);

        while (startIndex < endIndex && char.IsWhiteSpace(text[startIndex]))
        {
            startIndex++;
        }

        while (endIndex > startIndex && char.IsWhiteSpace(text[endIndex - 1]))
        {
            endIndex--;
        }

        return new TextSlice(startIndex, endIndex, text[startIndex..endIndex]);
    }

    private static int? AddOffset(int? sourceStartOffset, int relativeOffset)
    {
        return sourceStartOffset is null
            ? null
            : sourceStartOffset.Value + relativeOffset;
    }

    private sealed record ForcedSplitBlock(DocumentBlock Block, string CoreText, bool HasOverlap);

    private readonly record struct TextSlice(int StartIndex, int EndIndex, string Text);
}
