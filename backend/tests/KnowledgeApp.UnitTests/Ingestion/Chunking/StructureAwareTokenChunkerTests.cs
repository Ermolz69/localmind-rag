using KnowledgeApp.Application.Abstractions.Ingestion;
using KnowledgeApp.Application.Ingestion.IncrementalIndexing;
using KnowledgeApp.Infrastructure.Options;
using KnowledgeApp.Infrastructure.Services.Ingestion.Chunking;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.UnitTests.Ingestion.Chunking;

public class StructureAwareTokenChunkerTests
{
    [Fact]
    public void ChunkerConsistency_IdentityChangesWithMetadata_EmbeddingStaysSame()
    {
        var options = new ChunkingOptions { ChunkingAlgorithmId = "algo-1" };
        var monitor = new TestOptionsMonitor<ChunkingOptions>(options);

        var tokenizer = new TestTokenizer();
        var normalizer = new TestNormalizer();
        var hashService = new TestHashService();

        var parser1 = new TestParser([new DocumentBlock(DocumentBlockType.Paragraph, "Hello world", "doc", "H1", 0, 11, 2, false)]);
        var chunker1 = new StructureAwareTokenChunker(tokenizer, normalizer, [parser1], hashService, monitor);

        var chunks1 = chunker1.SplitDetailed("Hello world");
        var chunk1 = chunks1[0];

        var parser2 = new TestParser([new DocumentBlock(DocumentBlockType.Paragraph, "Hello world", "doc", "H2", 0, 11, 2, false)]);
        var chunker2 = new StructureAwareTokenChunker(tokenizer, normalizer, [parser2], hashService, monitor);

        var chunks2 = chunker2.SplitDetailed("Hello world");
        var chunk2 = chunks2[0];

        Assert.Equal($"hash-{chunk1.Text}", chunk1.EmbeddingTextHash);
        Assert.NotEqual(chunk1.ChunkIdentityHash, chunk2.ChunkIdentityHash);
        Assert.Equal(chunk1.EmbeddingTextHash, chunk2.EmbeddingTextHash);
    }

    [Fact]
    public void SplitDetailed_LongSingleBlock_SplitsAroundTargetTokensWithOverlapAndOffsets()
    {
        ChunkingOptions options = new()
        {
            ChunkingAlgorithmId = "algo-oversized-test",
            Default = new ChunkingProfile
            {
                TargetTokens = 5,
                MaxTokens = 8,
                MinTokens = 2,
                OverlapTokens = 2
            }
        };

        string text = string.Join(' ', Enumerable.Range(0, 14).Select(index => $"word{index:D2}"));
        var block = new DocumentBlock(
            DocumentBlockType.Paragraph,
            text,
            "root",
            "section",
            20,
            20 + text.Length,
            14,
            false);

        StructureAwareTokenChunker chunker = CreateChunker(options, [block]);

        IReadOnlyList<DocumentChunkText> chunks = chunker.SplitDetailed(text);

        Assert.Equal([5, 5, 5, 5], chunks.Select(chunk => chunk.TokenCount).ToArray());
        Assert.All(chunks, chunk => Assert.InRange(chunk.TokenCount, 1, options.Default.MaxTokens));
        Assert.False(chunks[0].HasOverlap);
        Assert.All(chunks.Skip(1), chunk => Assert.True(chunk.HasOverlap));
        Assert.All(chunks, chunk => Assert.NotNull(chunk.SourceStartOffset));
        Assert.All(chunks, chunk => Assert.NotNull(chunk.SourceEndOffset));
        Assert.True(chunks.Zip(chunks.Skip(1), (left, right) => left.SourceStartOffset <= right.SourceStartOffset).All(BooleanIdentity));
        Assert.Contains("word03", chunks[0].Text);
        Assert.Contains("word03", chunks[1].Text);
        Assert.DoesNotContain("word03", chunks[1].CoreText);
    }

    [Fact]
    public void SplitDetailed_NormalBlocks_FlushesAroundTargetWithoutForcedOverlap()
    {
        ChunkingOptions options = new()
        {
            ChunkingAlgorithmId = "algo-normal-test",
            Default = new ChunkingProfile
            {
                TargetTokens = 4,
                MaxTokens = 8,
                MinTokens = 2,
                OverlapTokens = 2
            }
        };

        DocumentBlock[] blocks =
        [
            Block("alpha beta", 0),
            Block("gamma delta", 11),
            Block("epsilon zeta", 24)
        ];

        StructureAwareTokenChunker chunker = CreateChunker(options, blocks);

        IReadOnlyList<DocumentChunkText> chunks = chunker.SplitDetailed(string.Join("\n\n", blocks.Select(block => block.Text)));

        Assert.Equal(2, chunks.Count);
        Assert.Equal("alpha beta\n\ngamma delta", chunks[0].Text);
        Assert.Equal("epsilon zeta", chunks[1].Text);
        Assert.All(chunks, chunk => Assert.False(chunk.HasOverlap));
        Assert.All(chunks, chunk => Assert.InRange(chunk.TokenCount, 1, options.Default.MaxTokens));
    }

    private static bool BooleanIdentity(bool value)
    {
        return value;
    }

    private static DocumentBlock Block(string text, int startOffset)
    {
        return new DocumentBlock(
            DocumentBlockType.Paragraph,
            text,
            null,
            null,
            startOffset,
            startOffset + text.Length,
            text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
            false);
    }

    private static StructureAwareTokenChunker CreateChunker(
        ChunkingOptions options,
        IReadOnlyList<DocumentBlock> blocks)
    {
        return new StructureAwareTokenChunker(
            new TestTokenizer(),
            new TestNormalizer(),
            [new TestParser(blocks)],
            new TestHashService(),
            new TestOptionsMonitor<ChunkingOptions>(options));
    }

    private sealed class TestOptionsMonitor<T> : IOptionsMonitor<T> where T : class
    {
        public TestOptionsMonitor(T currentValue) => CurrentValue = currentValue;
        public T CurrentValue { get; }
        public T Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }

    private sealed class TestTokenizer : ITokenizerService
    {
        private readonly Dictionary<int, string> tokensById = [];

        public string TokenizerId => "test-tokenizer";
        public bool IsAvailable => true;
        public void EnsureAvailable() { }

        public int CountTokens(string text)
        {
            return GetTokenSpans(text).Sum(span => span.TokenCount);
        }

        public string Decode(IReadOnlyList<int> tokens)
        {
            return string.Join(' ', tokens.Select(token => tokensById.TryGetValue(token, out string? value) ? value : $"token{token}"));
        }

        public IReadOnlyList<int> Encode(string text)
        {
            IReadOnlyList<TokenSpan> spans = GetTokenSpans(text);
            List<int> tokenIds = new(spans.Count);

            for (int index = 0; index < spans.Count; index++)
            {
                TokenSpan span = spans[index];
                int tokenId = tokenIds.Count;
                tokensById[tokenId] = text.Substring(span.StartIndex, span.Length);
                tokenIds.Add(tokenId);
            }

            return tokenIds;
        }

        public IReadOnlyList<TokenSpan> GetTokenSpans(string text)
        {
            List<TokenSpan> spans = [];
            int index = 0;

            while (index < text.Length)
            {
                while (index < text.Length && char.IsWhiteSpace(text[index]))
                {
                    index++;
                }

                int start = index;

                while (index < text.Length && !char.IsWhiteSpace(text[index]))
                {
                    index++;
                }

                if (index > start)
                {
                    spans.Add(new TokenSpan(start, index - start, 1));
                }
            }

            return spans;
        }
    }

    private sealed class TestNormalizer : IChunkTextNormalizer
    {
        public string NormalizeForEmbedding(string text) => text;
        public string NormalizeForIdentity(string text) => text;
    }

    private sealed class TestHashService : IContentHashService
    {
        public string ComputeChunkHash(string content) => $"hash-{content}";
        public string ComputeDocumentHash(IEnumerable<string> orderedChunkHashes, int indexVersion) => "doc-hash";
    }

    private sealed class TestParser : ITextStructureParser
    {
        private readonly IReadOnlyList<DocumentBlock> blocks;
        public TestParser(IReadOnlyList<DocumentBlock> blocks) => this.blocks = blocks;
        public bool CanParse(string text) => true;
        public IReadOnlyList<DocumentBlock> Parse(string text) => blocks;
    }
}
