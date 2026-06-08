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
        // Arrange
        var options = new ChunkingOptions { ChunkingAlgorithmId = "algo-1" };
        var monitor = new TestOptionsMonitor<ChunkingOptions>(options);

        var tokenizer = new TestTokenizer();
        var normalizer = new TestNormalizer();
        var hashService = new TestHashService();

        // First parse - with heading "H1"
        var parser1 = new TestParser(new DocumentBlock(DocumentBlockType.Paragraph, "Hello world", "doc", "H1", 0, 11, 10, false));
        var chunker1 = new StructureAwareTokenChunker(tokenizer, normalizer, [parser1], hashService, monitor);

        var chunks1 = chunker1.SplitDetailed("Hello world");
        var chunk1 = chunks1[0];

        // Second parse - different heading "H2"
        var parser2 = new TestParser(new DocumentBlock(DocumentBlockType.Paragraph, "Hello world", "doc", "H2", 0, 11, 10, false));
        var chunker2 = new StructureAwareTokenChunker(tokenizer, normalizer, [parser2], hashService, monitor);

        var chunks2 = chunker2.SplitDetailed("Hello world");
        var chunk2 = chunks2[0];

        // Assert
        // 1. EmbeddingTextHash is calculated from the exact same text used for embedding
        Assert.Equal($"hash-{chunk1.Text}", chunk1.EmbeddingTextHash);
        
        // 2. ChunkIdentityHash changes when HeadingPath changes
        Assert.NotEqual(chunk1.ChunkIdentityHash, chunk2.ChunkIdentityHash);
        
        // 3. EmbeddingTextHash does not change when only HeadingPath changes
        Assert.Equal(chunk1.EmbeddingTextHash, chunk2.EmbeddingTextHash);
    }

    private class TestOptionsMonitor<T> : IOptionsMonitor<T> where T : class
    {
        public TestOptionsMonitor(T currentValue) => CurrentValue = currentValue;
        public T CurrentValue { get; }
        public T Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }

    private class TestTokenizer : ITokenizerService
    {
        public string TokenizerId => "test-tokenizer";
        public bool IsAvailable => true;
        public void EnsureAvailable() { }
        public int CountTokens(string text) => 10;
        public string Decode(IReadOnlyList<int> tokens) => "Decoded";
        public IReadOnlyList<int> Encode(string text) => [1, 2, 3];
        public IReadOnlyList<TokenSpan> GetTokenSpans(string text) => [];
    }

    private class TestNormalizer : IChunkTextNormalizer
    {
        public string NormalizeForEmbedding(string text) => text;
        public string NormalizeForIdentity(string text) => text;
    }

    private class TestHashService : IContentHashService
    {
        public string ComputeChunkHash(string content) => $"hash-{content}";
        public string ComputeDocumentHash(Stream stream) => "doc-hash";
        public string ComputeDocumentHash(IEnumerable<string> filePaths, int bufferSize = 4096) => "doc-hash";
    }

    private class TestParser : ITextStructureParser
    {
        private readonly DocumentBlock _block;
        public TestParser(DocumentBlock block) => _block = block;
        public bool CanParse(string text) => true;
        public IReadOnlyList<DocumentBlock> Parse(string text) => [_block];
    }
}
